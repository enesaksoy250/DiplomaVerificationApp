const hre = require("hardhat");

async function main() {
  const registry = await hre.ethers.deployContract("DiplomaRegistry");
  await registry.waitForDeployment();

  console.log(`DiplomaRegistry deployed to ${await registry.getAddress()}`);
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
