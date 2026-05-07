const uploadForm = document.querySelector("#uploadForm");
const verifyForm = document.querySelector("#verifyForm");

wireFileName("#uploadFile", "#uploadFileName");
wireFileName("#verifyFile", "#verifyFileName");
wireDropState("#uploadFile");
wireDropState("#verifyFile");
wireCopyButtons();

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
    label.textContent = input.files?.[0]?.name || "PDF seç veya buraya sürükle";
  });
}

function wireDropState(inputSelector) {
  const input = document.querySelector(inputSelector);
  const zone = input?.closest(".file-zone");
  const label = zone?.querySelector(".file-title");

  if (!input || !zone) {
    return;
  }

  ["dragenter", "dragover"].forEach((eventName) => {
    zone.addEventListener(eventName, (event) => {
      event.preventDefault();
      zone.classList.add("drag-active");
    });
  });

  ["dragleave", "drop"].forEach((eventName) => {
    zone.addEventListener(eventName, (event) => {
      event.preventDefault();
      zone.classList.remove("drag-active");
    });
  });

  zone.addEventListener("drop", (event) => {
    const file = event.dataTransfer?.files?.[0];

    if (!file) {
      return;
    }

    const transfer = new DataTransfer();
    transfer.items.add(file);
    input.files = transfer.files;

    if (label) {
      label.textContent = file.name;
    }

    input.dispatchEvent(new Event("change", { bubbles: true }));
  });
}

async function submitUpload() {
  const form = document.querySelector("#uploadForm");
  const button = form.querySelector("button");
  const message = document.querySelector("#uploadMessage");
  const result = document.querySelector("#uploadResult");
  const details = document.querySelector("#uploadDetails");
  const empty = document.querySelector("#uploadEmpty");
  const qrPlaceholder = document.querySelector("#qrPlaceholder");
  const qrLink = document.querySelector("#qrLink");
  const qrImage = document.querySelector("#qrImage");

  setBusy(button, true);
  hide(message);
  show(result);

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
    qrImage.src = json.qrCodeDataUrl;
    hide(empty);
    show(details);
    hide(qrPlaceholder);
    show(qrLink);
    show(qrImage);
  } catch (error) {
    hide(details);
    show(empty);
    if (qrImage) {
      qrImage.removeAttribute("src");
      hide(qrImage);
    }
    hide(qrLink);
    show(qrPlaceholder);
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
    throw new Error(json.error || json.status || "İşlem başarısız.");
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

function wireCopyButtons() {
  document.addEventListener("click", async (event) => {
    const button = event.target.closest(".copy-button");

    if (!button) {
      return;
    }

    const target = document.querySelector(button.dataset.copyTarget);
    const text = target?.textContent?.trim();

    if (!text || text === "-") {
      return;
    }

    try {
      await navigator.clipboard.writeText(text);
      button.classList.add("copied");
      setTimeout(() => button.classList.remove("copied"), 900);
    } catch {
      button.classList.remove("copied");
    }
  });
}

function statusClass(status) {
  const value = (status || "").toLocaleLowerCase("tr-TR");

  if (value.includes("ersiz")) {
    return "status-badge status-invalid";
  }

  if (value.includes("bulun")) {
    return "status-badge status-missing";
  }

  if (value.includes("erli")) {
    return "status-badge status-valid";
  }

  return "status-badge status-invalid";
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
