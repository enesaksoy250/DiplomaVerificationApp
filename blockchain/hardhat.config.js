require("@nomicfoundation/hardhat-toolbox");
require("dotenv").config();

const sepoliaRpcUrl = process.env.SEPOLIA_RPC_URL || "";
const privateKey = process.env.PRIVATE_KEY || "";

module.exports = {
  solidity: {
    version: "0.8.24",
    settings: {
      optimizer: {
        enabled: true,
        runs: 200
      }
    }
  },
  networks: {
    sepolia: {
      url: sepoliaRpcUrl,
      accounts: privateKey ? [privateKey] : [],
      chainId: 11155111
    }
  }
};
