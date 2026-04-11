UPDATE dbo.Users SET PasswordHash = N'$2a$11$a0ppKaGBSwYDBIrugcILE.dZ1tj0uzQjaaOmzCw.tNN0XgBN.7E/6' WHERE Email = N'davila06@gmail.com';
SELECT @@ROWCOUNT AS Updated;
