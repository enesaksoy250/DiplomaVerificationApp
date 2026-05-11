// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

contract DiplomaRegistry {
    struct Diploma {
        bool exists;
        uint256 timestamp;
        bytes32 issuerId;
        bytes32 signatureHash;
        address registeredBy;
    }

    address public owner;
    mapping(bytes32 => Diploma) private diplomas;
    mapping(address => bool) public registrars;

    event RegistrarUpdated(address indexed registrar, bool allowed);
    event DiplomaRegistered(
        bytes32 indexed pdfHash,
        bytes32 indexed issuerId,
        bytes32 signatureHash,
        address indexed registeredBy,
        uint256 timestamp
    );

    modifier onlyOwner() {
        require(msg.sender == owner, "Only owner");
        _;
    }

    modifier onlyRegistrar() {
        require(registrars[msg.sender], "Not authorized registrar");
        _;
    }

    constructor() {
        owner = msg.sender;
        registrars[msg.sender] = true;
        emit RegistrarUpdated(msg.sender, true);
    }

    function setRegistrar(address registrar, bool allowed) external onlyOwner {
        require(registrar != address(0), "Invalid registrar");
        registrars[registrar] = allowed;
        emit RegistrarUpdated(registrar, allowed);
    }

    function registerDiploma(bytes32 pdfHash, bytes32 issuerId, bytes32 signatureHash) external onlyRegistrar {
        require(pdfHash != bytes32(0), "Invalid PDF hash");
        require(issuerId != bytes32(0), "Invalid issuer");
        require(signatureHash != bytes32(0), "Invalid signature");
        require(!diplomas[pdfHash].exists, "Diploma already registered");

        diplomas[pdfHash] = Diploma({
            exists: true,
            timestamp: block.timestamp,
            issuerId: issuerId,
            signatureHash: signatureHash,
            registeredBy: msg.sender
        });

        emit DiplomaRegistered(pdfHash, issuerId, signatureHash, msg.sender, block.timestamp);
    }

    function verifyDiploma(bytes32 pdfHash)
        external
        view
        returns (
            bool exists,
            uint256 timestamp,
            bytes32 issuerId,
            bytes32 signatureHash,
            address registeredBy
        )
    {
        Diploma memory diploma = diplomas[pdfHash];
        return (
            diploma.exists,
            diploma.timestamp,
            diploma.issuerId,
            diploma.signatureHash,
            diploma.registeredBy
        );
    }
}
