namespace ExamShield.Domain.Interfaces;

public interface IImageEncryptionService
{
    (byte[] Ciphertext, byte[] EncryptedDek) Encrypt(byte[] plaintext);
    byte[] Decrypt(byte[] ciphertext, byte[] encryptedDek);
}
