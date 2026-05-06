// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

contract DiplomaRegistry {
    struct Diploma {
        bool exists;
        uint256 timestamp;
    }

    mapping(bytes32 => Diploma) private diplomas;

    event DiplomaRegistered(bytes32 indexed pdfHash, uint256 timestamp);

    function registerDiploma(bytes32 pdfHash) external {
        require(pdfHash != bytes32(0), "Invalid PDF hash");
        require(!diplomas[pdfHash].exists, "Diploma already registered");

        diplomas[pdfHash] = Diploma({
            exists: true,
            timestamp: block.timestamp
        });

        emit DiplomaRegistered(pdfHash, block.timestamp);
    }

    function verifyDiploma(bytes32 pdfHash) external view returns (bool exists, uint256 timestamp) {
        Diploma memory diploma = diplomas[pdfHash];
        return (diploma.exists, diploma.timestamp);
    }
}
