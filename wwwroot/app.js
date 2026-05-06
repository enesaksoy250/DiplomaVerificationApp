const uploadForm = document.querySelector("#uploadForm");
const verifyForm = document.querySelector("#verifyForm");

wireFileName("#uploadFile", "#uploadFileName");
wireFileName("#verifyFile", "#verifyFileName");

if (uploadForm) {
  uploadForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    await submitUpload();
  });
}

if (verifyForm) {
  verifyForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    await submitVerify();
  });

  const hash = new URLSearchParams(window.location.search).get("hash");
  if (hash) {
    verifyHash(hash);
  }
}

function wireFileName(inputSelector, labelSelector) {
  const input = document.querySelector(inputSelector);
  const label = document.querySelector(labelSelector);

  if (!input || !label) {
    return;
  }

  input.addEventListener("change", () => {
    label.textContent = input.files?.[0]?.name || "PDF sec";
  });
}

async function submitUpload() {
  const form = document.querySelector("#uploadForm");
  const button = form.querySelector("button");
  const message = document.querySelector("#uploadMessage");
  const result = document.querySelector("#uploadResult");

  setBusy(button, true);
  hide(message);
  hide(result);

  try {
    const json = await sendForm("/upload", form);
    document.querySelector("#uploadStatus").textContent = json.status;
    document.querySelector("#uploadStatus").className = statusClass(json.status);
    document.querySelector("#uploadHash").textContent = json.pdfHash;
    document.querySelector("#uploadTx").textContent = json.transactionHash;
    document.querySelector("#uploadTimestamp").textContent = json.registeredAtUtc;
    document.querySelector("#uploadNetwork").textContent = json.network;
    document.querySelector("#uploadLink").textContent = json.verificationUrl;
    document.querySelector("#uploadLink").href = json.verificationUrl;
    document.querySelector("#qrLink").href = json.verificationUrl;
    document.querySelector("#qrImage").src = json.qrCodeDataUrl;
    show(result);
  } catch (error) {
    showMessage(message, error.message, true);
  } finally {
    setBusy(button, false);
  }
}

async function submitVerify() {
  const form = document.querySelector("#verifyForm");
  const button = form.querySelector("button");
  const message = document.querySelector("#verifyMessage");
  const result = document.querySelector("#verifyResult");

  setBusy(button, true);
  hide(message);
  hide(result);

  try {
    const json = await sendForm("/verify", form);
    renderVerifyResult(json);
  } catch (error) {
    showMessage(message, error.message, true);
  } finally {
    setBusy(button, false);
  }
}

async function verifyHash(hash) {
  const message = document.querySelector("#verifyMessage");
  const result = document.querySelector("#verifyResult");

  hide(message);
  hide(result);

  try {
    const response = await fetch(`/verification/${encodeURIComponent(hash)}`);
    const json = await parseResponse(response);
    renderVerifyResult(json);
  } catch (error) {
    showMessage(message, error.message, true);
  }
}

async function sendForm(url, form) {
  const response = await fetch(url, {
    method: "POST",
    body: new FormData(form)
  });

  return parseResponse(response);
}

async function parseResponse(response) {
  const json = await response.json().catch(() => ({}));

  if (!response.ok) {
    throw new Error(json.error || json.status || "Islem basarisiz.");
  }

  return json;
}

function renderVerifyResult(json) {
  document.querySelector("#verifyStatus").textContent = json.status;
  document.querySelector("#verifyStatus").className = statusClass(json.status);
  document.querySelector("#verifyHash").textContent = json.pdfHash;
  document.querySelector("#verifyTx").textContent = json.transactionHash || "-";
  document.querySelector("#verifyTimestamp").textContent = json.registeredAtUtc || "-";
  document.querySelector("#verifyNetwork").textContent = json.network;
  show(document.querySelector("#verifyResult"));
}

function statusClass(status) {
  if (status === "Geçerli Diploma") {
    return "status-valid";
  }

  if (status === "Blockchain Kaydı Bulunamadı") {
    return "status-missing";
  }

  return "status-invalid";
}

function show(element) {
  if (element) {
    element.hidden = false;
  }
}

function hide(element) {
  if (element) {
    element.hidden = true;
  }
}

function setBusy(button, isBusy) {
  if (button) {
    button.disabled = isBusy;
  }
}

function showMessage(element, text, isError) {
  if (!element) {
    return;
  }

  element.textContent = text;
  element.className = isError ? "message error" : "message";
  show(element);
}
