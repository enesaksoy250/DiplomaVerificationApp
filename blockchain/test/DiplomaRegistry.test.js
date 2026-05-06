const { expect } = require("chai");
const { ethers } = require("hardhat");
const { anyValue } = require("@nomicfoundation/hardhat-chai-matchers/withArgs");

describe("DiplomaRegistry", function () {
  it("registers and verifies a diploma hash", async function () {
    const registry = await ethers.deployContract("DiplomaRegistry");
    const hash = ethers.keccak256(ethers.toUtf8Bytes("sample-pdf-hash"));

    await expect(registry.registerDiploma(hash))
      .to.emit(registry, "DiplomaRegistered")
      .withArgs(hash, anyValue);

    const [exists, timestamp] = await registry.verifyDiploma(hash);

    expect(exists).to.equal(true);
    expect(timestamp).to.be.greaterThan(0);
  });

  it("rejects duplicate diploma records", async function () {
    const registry = await ethers.deployContract("DiplomaRegistry");
    const hash = ethers.keccak256(ethers.toUtf8Bytes("duplicate-pdf-hash"));

    await registry.registerDiploma(hash);

    await expect(registry.registerDiploma(hash)).to.be.revertedWith("Diploma already registered");
  });

  it("returns false for unknown hashes", async function () {
    const registry = await ethers.deployContract("DiplomaRegistry");
    const hash = ethers.keccak256(ethers.toUtf8Bytes("unknown-pdf-hash"));

    const [exists, timestamp] = await registry.verifyDiploma(hash);

    expect(exists).to.equal(false);
    expect(timestamp).to.equal(0);
  });
});
