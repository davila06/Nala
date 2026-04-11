using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PawTrack.API.Controllers;
using PawTrack.Application.Bot.Commands.HandleWhatsAppWebhook;
using PawTrack.Domain.Common;
using System.Text.Json;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-38 security regression tests.
///
/// Gap: <c>WhatsAppController.ReceiveWebhook</c> builds a contact-name lookup map with:
///
///   <code>
///   var contactMap = (change.Value?.Contacts ?? [])
///       .ToDictionary(c => c.WaId, c => ...);
///   </code>
///
/// <c>MetaContact.WaId</c> is declared as <c>string?</c> — it can be null if Meta
/// sends a contact object without the <c>wa_id</c> field (malformed payload, API
/// version mismatch, or a deliberately crafted webhook body that evades the HMAC
/// guard by using a known-valid signature over a null-key payload).
///
/// ── Impact ────────────────────────────────────────────────────────────────────
///   <c>Dictionary&lt;string, string&gt;.Add(null, ...)</c> throws
///   <c>ArgumentNullException</c> at runtime.  The outer <c>try/catch</c> in
///   <c>ReceiveWebhook</c> catches the exception and logs it, so Meta receives a
///   200 OK and does NOT retry.  However:
///
///   1. All messages in the same change batch are silently dropped — the crash
///      happens before any <c>foreach</c> over messages fires.
///   2. A crafted payload with a null-WaId contact + real messages can be used as
///      a message-delivery bypass: the attacker knows the HMAC secret (or finds
///      another way to produce a valid signature) and nullifies all inbound
///      processing for a target sender.
///   3. The <c>CS8714</c> compiler warning reveals the issue to any reviewer.
///
/// ── Fix ───────────────────────────────────────────────────────────────────────
///   Filter out contacts with null or empty <c>WaId</c> before calling
///   <c>ToDictionary</c>:
///
///   <code>
///   var contactMap = (change.Value?.Contacts ?? [])
///       .Where(c => !string.IsNullOrEmpty(c.WaId))
///       .ToDictionary(c => c.WaId!, c => ...);
///   </code>
///
///   This eliminates the <c>ArgumentNullException</c> and the compiler warning,
///   ensures messages co-batched with null-WaId contacts are still processed, and
///   removes a silent message-drop vector.
/// </summary>
public sealed class Round38SecurityRegressionTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a WhatsAppController and submits the given payload directly through
    /// the <c>ReceiveWebhook</c> action, bypassing the HMAC filter (unit-test scope).
    /// Returns the IActionResult for assertion.
    /// </summary>
    private static async Task<IActionResult> InvokeReceiveWebhook(
        WhatsAppController.MetaWebhookPayload payload)
    {
        var sender = Substitute.For<ISender>();

        // Stub HandleWhatsAppWebhookCommand to succeed so we can focus on dispatch
        sender.Send(Arg.Any<HandleWhatsAppWebhookCommand>(), Arg.Any<CancellationToken>())
              .Returns(Task.FromResult(Result.Success(Unit.Value)));

        var logger = NullLogger<WhatsAppController>.Instance;

        var controller = new WhatsAppController(sender, logger)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            },
        };

        return await controller.ReceiveWebhook(payload, CancellationToken.None);
    }

    // ── Null WaId contact — messages must still be processed ─────────────────

    [Fact]
    public async Task ReceiveWebhook_WhenContactHasNullWaId_ReturnsOkWithoutThrowing()
    {
        // Arrange — a change batch with a null-WaId contact AND a real message.
        // Before the fix this throws ArgumentNullException inside ReceiveWebhook,
        // which is caught and suppresses all message processing.
        var payload = new WhatsAppController.MetaWebhookPayload
        {
            Object = "whatsapp_business_account",
            Entry =
            [
                new WhatsAppController.MetaEntry
                {
                    Changes =
                    [
                        new WhatsAppController.MetaChange
                        {
                            Field = "messages",
                            Value = new WhatsAppController.MetaChangeValue
                            {
                                Contacts =
                                [
                                    // Malformed contact — WaId is null
                                    new WhatsAppController.MetaContact
                                    {
                                        WaId = null,
                                        Profile = new WhatsAppController.MetaProfile { Name = "Ghost" },
                                    }
                                ],
                                Messages =
                                [
                                    new WhatsAppController.MetaMessage
                                    {
                                        Id   = "wamid.abc123",
                                        From = "50688887777",
                                        Type = "text",
                                        Text = new WhatsAppController.MetaTextBody { Body = "Hola" },
                                    }
                                ],
                            },
                        }
                    ],
                }
            ],
        };

        // Act — must not throw; the outer try/catch must not be reached via Dictionary exception
        var act = async () => await InvokeReceiveWebhook(payload);

        await act.Should().NotThrowAsync(
            "a null WaId in the contacts array must not crash the webhook handler — " +
            "such contacts should be filtered out before ToDictionary is called");
    }

    [Fact]
    public async Task ReceiveWebhook_WhenContactHasNullWaId_ReturnsHttp200()
    {
        // Arrange
        var payload = new WhatsAppController.MetaWebhookPayload
        {
            Object = "whatsapp_business_account",
            Entry =
            [
                new WhatsAppController.MetaEntry
                {
                    Changes =
                    [
                        new WhatsAppController.MetaChange
                        {
                            Field = "messages",
                            Value = new WhatsAppController.MetaChangeValue
                            {
                                Contacts =
                                [
                                    new WhatsAppController.MetaContact { WaId = null },
                                ],
                                Messages =
                                [
                                    new WhatsAppController.MetaMessage
                                    {
                                        Id   = "wamid.xyz789",
                                        From = "50688886666",
                                        Type = "text",
                                        Text = new WhatsAppController.MetaTextBody { Body = "Test" },
                                    }
                                ],
                            },
                        }
                    ],
                }
            ],
        };

        // Act
        var result = await InvokeReceiveWebhook(payload);

        // Assert — Meta always expects 200 OK regardless of payload content
        result.Should().BeOfType<OkResult>(
            "ReceiveWebhook must return 200 OK even when some contacts have null WaId fields");
    }

    [Fact]
    public async Task ReceiveWebhook_WhenContactHasNullWaId_StillDispatchesMessageCommand()
    {
        // Arrange — verify that message processing is NOT silently dropped
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<HandleWhatsAppWebhookCommand>(), Arg.Any<CancellationToken>())
              .Returns(Task.FromResult(Result.Success(Unit.Value)));

        var controller = new WhatsAppController(sender, NullLogger<WhatsAppController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            },
        };

        var payload = new WhatsAppController.MetaWebhookPayload
        {
            Object = "whatsapp_business_account",
            Entry =
            [
                new WhatsAppController.MetaEntry
                {
                    Changes =
                    [
                        new WhatsAppController.MetaChange
                        {
                            Field = "messages",
                            Value = new WhatsAppController.MetaChangeValue
                            {
                                Contacts =
                                [
                                    // null-WaId contact — must be filtered, not crash
                                    new WhatsAppController.MetaContact { WaId = null },
                                    // valid contact
                                    new WhatsAppController.MetaContact { WaId = "50688885555", Profile = new WhatsAppController.MetaProfile { Name = "Denis" } },
                                ],
                                Messages =
                                [
                                    new WhatsAppController.MetaMessage
                                    {
                                        Id   = "wamid.real1",
                                        From = "50688885555",
                                        Type = "text",
                                        Text = new WhatsAppController.MetaTextBody { Body = "Vi tu mascota" },
                                    }
                                ],
                            },
                        }
                    ],
                }
            ],
        };

        // Act
        await controller.ReceiveWebhook(payload, CancellationToken.None);

        // Assert — the real message must have been dispatched even though a null-WaId contact was present
        await sender.Received(1)
                    .Send(
                        Arg.Is<HandleWhatsAppWebhookCommand>(cmd =>
                            cmd.WaId == "50688885555" && cmd.TextBody == "Vi tu mascota"),
                        Arg.Any<CancellationToken>());
    }

    // ── Normal payload (no null WaId) — sanity baseline ─────────────────────

    [Fact]
    public async Task ReceiveWebhook_WhenAllContactsHaveValidWaId_DispatchesCommand()
    {
        // Arrange — normal payload to confirm no regression in the happy path
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<HandleWhatsAppWebhookCommand>(), Arg.Any<CancellationToken>())
              .Returns(Task.FromResult(Result.Success(Unit.Value)));

        var controller = new WhatsAppController(sender, NullLogger<WhatsAppController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        var payload = new WhatsAppController.MetaWebhookPayload
        {
            Object = "whatsapp_business_account",
            Entry =
            [
                new WhatsAppController.MetaEntry
                {
                    Changes =
                    [
                        new WhatsAppController.MetaChange
                        {
                            Field = "messages",
                            Value = new WhatsAppController.MetaChangeValue
                            {
                                Contacts =
                                [
                                    new WhatsAppController.MetaContact { WaId = "50688884444", Profile = new WhatsAppController.MetaProfile { Name = "Ana" } },
                                ],
                                Messages =
                                [
                                    new WhatsAppController.MetaMessage
                                    {
                                        Id   = "wamid.happy",
                                        From = "50688884444",
                                        Type = "text",
                                        Text = new WhatsAppController.MetaTextBody { Body = "Hola Ana" },
                                    }
                                ],
                            },
                        }
                    ],
                }
            ],
        };

        await controller.ReceiveWebhook(payload, CancellationToken.None);

        await sender.Received(1)
                    .Send(
                        Arg.Is<HandleWhatsAppWebhookCommand>(cmd => cmd.WaId == "50688884444"),
                        Arg.Any<CancellationToken>());
    }
}
