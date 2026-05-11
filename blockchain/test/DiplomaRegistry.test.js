const { expect } = require("chai");
const { ethers } = require("hardhat");
const { anyValue } = require("@nomicfoundation/hardhat-chai-matchers/withArgs");

describe("DiplomaRegistry", function () {
  const issuerId = ethers.keccak256(ethers.toUtf8Bytes("university-1"));
  const signatureHash = ethers.keccak256(ethers.toUtf8Bytes("signature-1"));

  it("registers and verifies a diploma hash", async function () {
    const [owner] = await ethers.getSigners();
    const registry = await ethers.deployContract("DiplomaRegistry");
    const hash = ethers.keccak256(ethers.toUtf8Bytes("sample-pdf-hash"));

    await expect(registry.registerDiploma(hash, issuerId, signatureHash))
      .to.emit(registry, "DiplomaRegistered")
      .withArgs(hash, issuerId, signatureHash, owner.address, anyValue);

    const [exists, timestamp, storedIssuerId, storedSignatureHash, registeredBy] = await registry.verifyDiploma(hash);

    expect(exists).to.equal(true);
    expect(timestamp).to.be.greaterThan(0);
    expect(storedIssuerId).to.equal(issuerId);
    expect(storedSignatureHash).to.equal(signatureHash);
    expect(registeredBy).to.equal(owner.address);
  });

  it("rejects duplicate diploma records", async function () {
    const registry = await ethers.deployContract("DiplomaRegistry");
    const hash = ethers.keccak256(ethers.toUtf8Bytes("duplicate-pdf-hash"));

    await registry.registerDiploma(hash, issuerId, signatureHash);

    await expect(registry.registerDiploma(hash, issuerId, signatureHash)).to.be.revertedWith("Diploma already registered");
  });

  it("returns false for unknown hashes", async function () {
    const registry = await ethers.deployContract("DiplomaRegistry");
    const hash = ethers.keccak256(ethers.toUtf8Bytes("unknown-pdf-hash"));

    const [exists, timestamp, storedIssuerId, storedSignatureHash, registeredBy] = await registry.verifyDiploma(hash);

    expect(exists).to.equal(false);
    expect(timestamp).to.equal(0);
    expect(storedIssuerId).to.equal(ethers.ZeroHash);
    expect(storedSignatureHash).to.equal(ethers.ZeroHash);
    expect(registeredBy).to.equal(ethers.ZeroAddress);
  });

  it("rejects registration from unauthorized accounts", async function () {
    const [, unauthorized] = await ethers.getSigners();
    const registry = await ethers.deployContract("DiplomaRegistry");
    const hash = ethers.keccak256(ethers.toUtf8Bytes("unauthorized-pdf-hash"));

    await expect(
      registry.connect(unauthorized).registerDiploma(hash, issuerId, signatureHash)
    ).to.be.revertedWith("Not authorized registrar");
  });

  it("allows owner to authorize a registrar", async function () {
    const [, registrar] = await ethers.getSigners();
    const registry = await ethers.deployContract("DiplomaRegistry");
    const hash = ethers.keccak256(ethers.toUtf8Bytes("registrar-pdf-hash"));

    await expect(registry.setRegistrar(registrar.address, true))
      .to.emit(registry, "RegistrarUpdated")
      .withArgs(registrar.address, true);

    await expect(registry.connect(registrar).registerDiploma(hash, issuerId, signatureHash))
      .to.emit(registry, "DiplomaRegistered")
      .withArgs(hash, issuerId, signatureHash, registrar.address, anyValue);
  });
});
