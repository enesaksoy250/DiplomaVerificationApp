# Blockchain Tabanli Diploma Dogrulama Sistemi - Proje Plani

Bu proje, `Blockchain Tabanli Diploma Dogrulama Sistemi.pdf` teknik sartnamesine sadik kalinarak gelistirilecektir.

## 1. MVP Kapsami

- PDF formatindaki diplomalar yuklenecek.
- PDF dosyasinin SHA256 hash degeri uretilecek.
- Blockchain'e yalnizca PDF hash degeri ve timestamp kaydedilecek.
- PDF icerigi blockchain'e veya kalici dosya depolamaya yazilmayacak.
- Ayni PDF dosyasinin blockchain uzerinden dogrulanmasi saglanacak.
- Kayit sonrasi benzersiz dogrulama baglantisi ve bu baglantiya giden QR kod uretilecek.

## 2. Backend Gorevleri

- `POST /upload`
  - PDF upload kabul eder.
  - Yalnizca PDF dosyalarini isler.
  - 0 byte, bos icerikli ve 10 MB ustu dosyalari reddeder.
  - SHA256 hash uretir.
  - Hash'i Nethereum ile `bytes32` formatinda smart contract'a gonderir.
  - Transaction hash, blockchain timestamp, ag bilgisi, dogrulama linki ve QR kod dondurur.
- `POST /verify`
  - PDF upload kabul eder.
  - SHA256 hash uretir.
  - Blockchain uzerinde hash kaydini kontrol eder.
  - `Gecerli Diploma`, `Gecersiz Diploma` veya `Blockchain Kaydi Bulunamadi` sonucunu dondurur.
- `GET /verification/{hash}`
  - QR kodun yonlendirdigi benzersiz dogrulama URL'sinin API karsiligidir.
  - Hash uzerinden blockchain kaydini kontrol eder.

## 3. Smart Contract Gorevleri

- Repo icinde Hardhat duzeni kurulacak.
- Sepolia test agi kullanilacak.
- Sartnamedeki veri yapisi birebir uygulanacak:

```solidity
struct Diploma {
    bool exists;
    uint256 timestamp;
}
```

- `mapping(bytes32 => Diploma)` kullanilacak.
- `registerDiploma(bytes32 pdfHash)` duplicate kayitlari engelleyecek.
- `verifyDiploma(bytes32 pdfHash)` kayit durumunu ve timestamp bilgisini dondurecek.
- Timestamp icin `block.timestamp` kullanilacak.
- Deploy/test icin Sepolia Faucet uzerinden Test ETH temin edilecek.

## 4. Frontend Gorevleri

- ASP.NET projesi icinde basit upload ve verify ekranlari hazirlanacak.
- Upload ekraninda PDF upload, islem sonucu, transaction hash, timestamp, ag bilgisi, verification link ve QR kod gosterilecek.
- Verify ekraninda PDF upload, sonuc, timestamp, transaction hash ve ag bilgisi gosterilecek.
- Tarih formati sartnamedeki ornege uygun gosterilecek: `12 Mayis 2026 14:32 UTC`.
- QR kod benzersiz dogrulama URL'sine yonlendirecek.

## 5. Guvenlik ve Altyapi

- Private key, RPC URL, Alchemy/Infura API anahtari gibi sirlar koda gomulmeyecek.
- Hardhat tarafinda `.env`, ASP.NET tarafinda local secret/config dosyalari kullanilacak.
- Secret iceren dosyalar `.gitignore` kapsaminda tutulacak.
- GitHub'a gercek private key veya API anahtari yuklenmeyecek.

## 6. Test Senaryolari

- Ayni PDF her zaman ayni SHA256 hash'i uretmeli.
- PDF olmayan dosyalar reddedilmeli.
- 0 byte ve bos icerikli PDF dosyalari reddedilmeli.
- 10 MB ustu dosyalar reddedilmeli.
- Hex SHA256 hash contract cagrisi oncesinde 32 byte `bytes32` formatina donusturulmeli.
- Unix timestamp C# tarafinda `DateTimeOffset.FromUnixTimeSeconds` ile UTC tarihe cevrilmeli.
- Kayit sonrasi transaction hash, timestamp, ag bilgisi, dogrulama baglantisi ve QR kod donmeli.
- Ayni PDF tekrar kaydedilmeye calisildiginda duplicate hatasi donmeli.
- Degistirilmis PDF dogrulamada gecersiz olmali.
- Blockchain kaydi olmayan PDF icin `Blockchain Kaydi Bulunamadi` sonucu donmeli.
