import { useMutation } from '@tanstack/react-query'
import { handoverApi } from '../api/handoverApi'
import { fraudApi, type ReportFraudPayload } from '../api/fraudApi'

/** Owner: generates a 4-digit handover code for safe physical pet handover. */
export function useGenerateHandoverCode() {
  return useMutation({
    mutationFn: (lostPetEventId: string) => handoverApi.generateCode(lostPetEventId),
  })
}

/** Rescuer: submits the code received verbally from the owner. */
export function useVerifyHandoverCode() {
  return useMutation({
    mutationFn: ({ lostPetEventId, code }: { lostPetEventId: string; code: string }) =>
      handoverApi.verifyCode(lostPetEventId, code),
  })
}

/** Any user (or anonymous): reports a fraud/scam attempt. */
export function useReportFraud() {
  return useMutation({
    mutationFn: (payload: ReportFraudPayload) => fraudApi.reportFraud(payload),
  })
}
