# Blockchain Tabanlı Diploma Doğrulama Sistemi - Proje Planı

Bu proje, `Blockchain Tabanlı Diploma Doğrulama Sistemi.pdf` teknik şartnamesine sadık kalınarak geliştirilecektir.

## 1. MVP Kapsamı

- PDF formatındaki diplomalar yüklenecek.
- PDF dosyasının SHA256 hash değeri üretilecek.
- Blockchain'e yalnızca PDF hash değeri ve timestamp kaydedilecek.
- PDF içeriği blockchain'e veya kalıcı dosya depolamaya yazılmayacak.
- Aynı PDF dosyasının blockchain üzerinden doğrulanması sağlanacak.
- Kayıt sonrası benzersiz doğrulama bağlantısı ve bu bağlantıya giden QR kod üretilecek.

## 2. Backend Görevleri

- `POST /upload`
  - PDF upload kabul eder.
  - Yalnızca PDF dosyalarını işler.
  - 0 byte, boş içerikli ve 10 MB üstü dosyaları reddeder.
  - SHA256 hash üretir.
  - Hash'i Nethereum ile `bytes32` formatında smart contract'a gönderir.
  - Transaction hash, blockchain timestamp, ağ bilgisi, doğrulama linki ve QR kod döndürür.
- `POST /verify`
  - PDF upload kabul eder.
  - SHA256 hash üretir.
  - Blockchain üzerinde hash kaydını kontrol eder.
  - `Geçerli Diploma`, `Geçersiz Diploma` veya `Blockchain Kaydı Bulunamadı` sonucunu döndürür.
- `GET /verification/{hash}`
  - QR kodun yönlendirdiği benzersiz doğrulama URL'sinin API karşılığıdır.
  - Hash üzerinden blockchain kaydını kontrol eder.

## 3. Smart Contract Görevleri

- Repo içinde Hardhat düzeni kurulacak.
- Sepolia test ağı kullanılacak.
- Şartnamedeki veri yapısı birebir uygulanacak:

```solidity
struct Diploma {
    bool exists;
    uint256 timestamp;
}
```

- `mapping(bytes32 => Diploma)` kullanılacak.
- `registerDiploma(bytes32 pdfHash)` duplicate kayıtları engelleyecek.
- `verifyDiploma(bytes32 pdfHash)` kayıt durumunu ve timestamp bilgisini döndürecek.
- Timestamp için `block.timestamp` kullanılacak.
- Deploy/test için Sepolia Faucet üzerinden Test ETH temin edilecek.

## 4. Frontend Görevleri

- ASP.NET projesi içinde basit upload ve verify ekranları hazırlanacak.
- Upload ekranında PDF upload, işlem sonucu, transaction hash, timestamp, ağ bilgisi, verification link ve QR kod gösterilecek.
- Verify ekranında PDF upload, sonuç, timestamp, transaction hash ve ağ bilgisi gösterilecek.
- Tarih formatı şartnamedeki örneğe uygun gösterilecek: `12 Mayıs 2026 14:32 UTC`.
- QR kod benzersiz doğrulama URL'sine yönlendirecek.

## 5. Güvenlik ve Altyapı

- Private key, RPC URL, Alchemy/Infura API anahtarı gibi sırlar koda gömülmeyecek.
- Hardhat tarafında `.env`, ASP.NET tarafında local secret/config dosyaları kullanılacak.
- Secret içeren dosyalar `.gitignore` kapsamında tutulacak.
- GitHub'a gerçek private key veya API anahtarı yüklenmeyecek.

## 6. Test Senaryoları

- Aynı PDF her zaman aynı SHA256 hash'i üretmeli.
- PDF olmayan dosyalar reddedilmeli.
- 0 byte ve boş içerikli PDF dosyaları reddedilmeli.
- 10 MB üstü dosyalar reddedilmeli.
- Hex SHA256 hash contract çağrısı öncesinde 32 byte `bytes32` formatına dönüştürülmeli.
- Unix timestamp C# tarafında `DateTimeOffset.FromUnixTimeSeconds` ile UTC tarihe çevrilmeli.
- Kayıt sonrası transaction hash, timestamp, ağ bilgisi, doğrulama bağlantısı ve QR kod dönmeli.
- Aynı PDF tekrar kaydedilmeye çalışıldığında duplicate hatası dönmeli.
- Değiştirilmiş PDF doğrulamada geçersiz olmalı.
- Blockchain kaydı olmayan PDF için `Blockchain Kaydı Bulunamadı` sonucu dönmeli.
