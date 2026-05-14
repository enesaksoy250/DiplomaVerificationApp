const uploadForm = document.querySelector("#uploadForm");
const verifyForm = document.querySelector("#verifyForm");
const loginForm = document.querySelector("#loginForm");
const universityForm = document.querySelector("#universityForm");
const userForm = document.querySelector("#userForm");
const studentCreationForm = document.querySelector("#studentCreationForm");
let adminUniversities = [];

wireFileName("#uploadFile", "#uploadFileName");
wireFileName("#verifyFile", "#verifyFileName");
wireDropState("#uploadFile");
wireDropState("#verifyFile");
wireCopyButtons();
wireFormResets();
wireAuthState();
wireUserRoleFields();

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

if (loginForm) {
  loginForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    await submitLogin();
  });
}

document.addEventListener("click", async (event) => {
  if (event.target.closest("#logoutButton")) {
    await fetch("/auth/logout", { method: "POST" });
    window.location.href = "/login.html";
  }
});

document.addEventListener("click", (event) => {
  const button = event.target.closest("[data-select-university]");

  if (!button) {
    return;
  }

  selectUniversityForUser(button.dataset.selectUniversity || "");
});

if (universityForm) {
  universityForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    await submitUniversity();
  });
}

if (userForm) {
  userForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    await submitUser();
  });
}

if (studentCreationForm) {
  studentCreationForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    await submitUniversityStudent();
  });
}

if (document.querySelector("#studentDiplomas")) {
  loadStudentDiplomas();
}

if (document.querySelector("#universitySelect")) {
  loadAdminUniversities();
}

const universitySearch = document.querySelector("#universitySearch");
if (universitySearch) {
  universitySearch.addEventListener("input", () => {
    renderAdminUniversityList(adminUniversities, universitySearch.value);
  });
}

if (document.querySelector("#studentAccountSelect")) {
  loadUniversityStudents();
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
  const button = form.querySelector("button[type='submit']");
  const message = document.querySelector("#uploadMessage");
  const result = document.querySelector("#uploadResult");
  const details = document.querySelector("#uploadDetails");
  const empty = document.querySelector("#uploadEmpty");
  const qrPlaceholder = document.querySelector("#qrPlaceholder");
  const qrLink = document.querySelector("#qrLink");
  const qrImage = document.querySelector("#qrImage");

  setBusy(button, true);
  showLoadingMessage(message, "Blockchain onaylanıyor...");
  show(result);
  hide(details);
  show(empty);

  try {
    const json = await sendForm("/upload", form);
    document.querySelector("#uploadStatus").textContent = json.status;
    document.querySelector("#uploadStatus").className = statusClass(json.status);
    document.querySelector("#uploadHash").textContent = json.pdfHash;
    document.querySelector("#uploadTx").textContent = json.transactionHash;
    document.querySelector("#uploadTimestamp").textContent = json.registeredAtUtc;
    document.querySelector("#uploadNetwork").textContent = json.network;
    document.querySelector("#uploadUniversity").textContent = json.universityName;
    document.querySelector("#uploadStudent").textContent = json.studentIdentifier;
    document.querySelector("#uploadSignatureStatus").textContent = json.signatureValid ? "Geçerli" : "Geçersiz";
    toggleTrustBadge("#uploadTrustBadge", json.status, json.signatureValid);
    document.querySelector("#uploadLink").textContent = json.verificationUrl;
    document.querySelector("#uploadLink").href = json.verificationUrl;
    document.querySelector("#qrLink").href = json.verificationUrl;
    qrImage.src = json.qrCodeDataUrl;
    hide(empty);
    show(details);
    hide(qrPlaceholder);
    show(qrLink);
    show(qrImage);
    await loadUniversityStudents();
    hide(message);
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
  const button = form.querySelector("button[type='submit']");
  const message = document.querySelector("#verifyMessage");
  const result = document.querySelector("#verifyResult");

  setBusy(button, true);
  showLoadingMessage(message, "Blockchain kaydı kontrol ediliyor...");
  hide(result);

  try {
    const json = await sendForm("/verify", form);
    renderVerifyResult(json);
    hide(message);
  } catch (error) {
    showMessage(message, error.message, true);
  } finally {
    setBusy(button, false);
  }
}

async function verifyHash(hash) {
  const message = document.querySelector("#verifyMessage");
  const result = document.querySelector("#verifyResult");

  showLoadingMessage(message, "Blockchain kaydı kontrol ediliyor...");
  hide(result);

  try {
    const response = await fetch(`/verification/${encodeURIComponent(hash)}`);
    const json = await parseResponse(response);
    renderVerifyResult(json);
    hide(message);
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

async function sendJson(url, body) {
  const response = await fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body)
  });

  return parseResponse(response);
}

async function parseResponse(response) {
  const json = await response.json().catch(() => ({}));

  if (!response.ok) {
    if (json.error || json.status) {
      throw new Error(json.error || json.status);
    }

    if (response.status === 401) {
      throw new Error("Bu işlem için giriş yapmalısınız.");
    }

    if (response.status === 403) {
      throw new Error("Bu işlem için yetkiniz yok.");
    }

    if (response.status === 404) {
      throw new Error("İstek adresi bulunamadı. Uygulamayı yeniden başlatıp tekrar deneyin.");
    }

    if (response.status === 405) {
      throw new Error("Bu işlem için gerekli endpoint güncel değil. Uygulamayı yeniden başlatıp tekrar deneyin.");
    }

    throw new Error("İşlem başarısız.");
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
  setText("#verifyUniversity", json.universityName || "-");
  setText("#verifyStudent", json.studentIdentifier || "-");
  setText("#verifySignatureStatus", json.signatureValid ? "Geçerli" : "Geçersiz");
  setText("#verifySignatureHash", json.signatureHash || "-");
  toggleTrustBadge("#verifyTrustBadge", json.status, json.signatureValid);
  show(document.querySelector("#verifyResult"));
}

async function submitLogin() {
  const message = document.querySelector("#loginMessage");
  hide(message);

  try {
    const formData = new FormData(loginForm);
    await sendJson("/auth/login", {
      email: formData.get("email"),
      password: formData.get("password")
    });
    window.location.href = getSafeReturnUrl() || "/verify.html";
  } catch (error) {
    showMessage(message, error.message, true);
  }
}

function getSafeReturnUrl() {
  const returnUrl = new URLSearchParams(window.location.search).get("returnUrl");

  if (!returnUrl || !returnUrl.startsWith("/") || returnUrl.startsWith("//")) {
    return null;
  }

  return returnUrl;
}

async function submitUniversity() {
  const message = document.querySelector("#adminMessage");
  hide(message);

  try {
    const formData = new FormData(universityForm);
    const university = await sendJson("/admin/universities", { name: formData.get("name") });
    const keyedUniversity = await sendJson(`/admin/universities/${encodeURIComponent(university.id)}/keys`, {});
    showMessage(message, `Üniversite oluşturuldu ve yetkili formuna seçildi: ${keyedUniversity.name}`, false);
    universityForm.reset();
    await loadAdminUniversities(keyedUniversity.id);
    selectUniversityForUser(keyedUniversity.id);
  } catch (error) {
    showMessage(message, error.message, true);
  }
}

async function submitUser() {
  const message = document.querySelector("#adminMessage");
  hide(message);

  try {
    const formData = new FormData(userForm);
    const role = formData.get("role");
    const requiresUniversity = role === "University";

    await sendJson("/admin/users", {
      email: formData.get("email"),
      password: formData.get("password"),
      role,
      universityId: requiresUniversity ? formData.get("universityId") || null : null,
      studentIdentifier: null
    });
    showMessage(message, "Kurum kullanıcısı oluşturuldu.", false);
    userForm.reset();
    updateUserRoleFields();
  } catch (error) {
    showMessage(message, error.message, true);
  }
}

async function submitUniversityStudent() {
  const message = document.querySelector("#studentCreationMessage");
  hide(message);

  try {
    const formData = new FormData(studentCreationForm);
    const student = await sendJson("/university/students", {
      email: formData.get("email"),
      password: formData.get("password"),
      studentIdentifier: formData.get("studentIdentifier")
    });

    showMessage(message, `Öğrenci hesabı oluşturuldu: ${student.email}`, false);
    studentCreationForm.reset();
    await loadUniversityStudents();
  } catch (error) {
    showMessage(message, error.message, true);
  }
}

function wireUserRoleFields() {
  if (!userForm) {
    return;
  }

  const roleSelect = userForm.querySelector("select[name='role']");
  roleSelect?.addEventListener("change", updateUserRoleFields);
  updateUserRoleFields();
}

function updateUserRoleFields() {
  if (!userForm) {
    return;
  }

  const role = userForm.querySelector("select[name='role']")?.value;
  const universityField = document.querySelector("#userUniversityField");
  const studentField = document.querySelector("#studentIdentifierField");
  const universitySelect = userForm.querySelector("select[name='universityId']");
  const studentInput = userForm.querySelector("input[name='studentIdentifier']");
  const shouldShowUniversity = role === "University";
  const shouldShowStudent = false;

  toggleField(universityField, universitySelect, shouldShowUniversity);
  toggleField(studentField, studentInput, shouldShowStudent);
}

function toggleField(field, input, shouldShow) {
  if (field) {
    field.hidden = !shouldShow;
  }

  if (!input) {
    return;
  }

  input.required = shouldShow;

  if (!shouldShow) {
    input.value = "";
  }
}

async function loadAdminUniversities(selectedUniversityId = "") {
  const select = document.querySelector("#universitySelect");
  const list = document.querySelector("#universityList");
  const search = document.querySelector("#universitySearch");

  try {
    const response = await fetch("/admin/universities");
    const universities = await parseResponse(response);
    adminUniversities = universities;

    if (select) {
      select.innerHTML = '<option value="">Üniversite seçin</option>';
      for (const university of universities) {
        const option = document.createElement("option");
        option.value = university.id;
        option.textContent = `${university.name} (${university.id})`;
        select.appendChild(option);
      }

      if (selectedUniversityId) {
        select.value = selectedUniversityId;
      }
    }

    if (list) {
      renderAdminUniversityList(universities, search?.value || "");
    }
  } catch (error) {
    const message = document.querySelector("#adminMessage");
    showMessage(message, error.message, true);
  }
}

function renderAdminUniversityList(universities, query = "") {
  const list = document.querySelector("#universityList");
  if (!list) {
    return;
  }

  const normalizedQuery = query.trim().toLocaleLowerCase("tr-TR");
  const filteredUniversities = normalizedQuery
    ? universities.filter((university) =>
        university.name.toLocaleLowerCase("tr-TR").includes(normalizedQuery) ||
        university.id.toLocaleLowerCase("tr-TR").includes(normalizedQuery))
    : universities;

  list.innerHTML = "";
  if (!filteredUniversities.length) {
    list.textContent = universities.length ? "Arama kriterine uygun üniversite yok." : "Henüz üniversite kaydı yok.";
    return;
  }

  for (const university of filteredUniversities) {
    const item = document.createElement("article");
    item.className = "list-item compact university-list-item";
    item.innerHTML = `
      <div class="list-item-main">
        <strong>${escapeHtml(university.name)}</strong>
        <span class="status-badge ${university.hasKeyPair ? "status-valid" : "status-missing"}">
          ${university.hasKeyPair ? "Blockchain Onaylı" : "Anahtar Bekleniyor"}
        </span>
      </div>
      <div class="university-id-row">
        <span class="hash-badge" id="universityId-${escapeHtml(university.id)}">${escapeHtml(university.id)}</span>
        <button class="copy-button" type="button" data-copy-target="#universityId-${escapeHtml(university.id)}" aria-label="Kurum ID kopyala"></button>
      </div>
      <div class="list-actions">
        <button class="secondary-button compact-action" type="button" data-select-university="${escapeHtml(university.id)}">+ Yetkili Ekle</button>
      </div>
    `;
    list.appendChild(item);
  }
}

function selectUniversityForUser(universityId) {
  const roleSelect = userForm?.querySelector("select[name='role']");
  const universitySelect = userForm?.querySelector("select[name='universityId']");
  const emailInput = userForm?.querySelector("input[name='email']");

  if (roleSelect) {
    roleSelect.value = "University";
  }

  updateUserRoleFields();

  if (universitySelect) {
    universitySelect.value = universityId;
  }

  emailInput?.focus();
}

async function loadUniversityStudents() {
  const select = document.querySelector("#studentAccountSelect");

  if (!select) {
    return;
  }

  try {
    const response = await fetch("/university/students");
    const students = await parseResponse(response);

    select.innerHTML = '<option value="">Öğrenci seçin</option>';
    for (const student of students) {
      const option = document.createElement("option");
      option.value = student.studentIdentifier;
      const diplomaStatus = student.hasDiploma
        ? `Diploma kayıtlı (${student.diplomaCount})`
        : "Diploma yok";
      option.textContent = `${student.studentIdentifier} - ${student.email || "E-posta yok"} - ${diplomaStatus}`;
      select.appendChild(option);
    }

    select.onchange = () => {
      setTextInputValue("#uploadStudentIdentifier", select.value);
    };
  } catch (error) {
    const option = document.createElement("option");
    option.value = "";
    option.textContent = error.message;
    select.innerHTML = "";
    select.appendChild(option);
  }
}

async function loadStudentDiplomas() {
  const list = document.querySelector("#studentDiplomas");
  const message = document.querySelector("#studentMessage");

  try {
    const response = await fetch("/student/diplomas");
    const diplomas = await parseResponse(response);
    list.innerHTML = "";

    if (!diplomas.length) {
      list.textContent = "Kayıtlı diploma bulunamadı.";
      return;
    }

    for (const diploma of diplomas) {
      const item = document.createElement("article");
      item.className = "list-item";
      item.innerHTML = `
        <strong>${escapeHtml(diploma.universityName || "Üniversite")}</strong>
        <span>${escapeHtml(diploma.registeredAtUtc)}</span>
        <code>${escapeHtml(diploma.pdfHash)}</code>
      `;
      list.appendChild(item);
    }
  } catch (error) {
    showMessage(message, error.message, true);
  }
}

async function wireAuthState() {
  const nav = document.querySelector(".nav");

  if (!nav) {
    return;
  }

  const response = await fetch("/auth/me").catch(() => null);
  const session = response?.ok ? await response.json().catch(() => null) : null;
  renderNavigation(nav, session);
}

function renderNavigation(nav, session) {
  const roles = session?.roles || [];
  const authenticated = Boolean(session?.authenticated);
  const links = [];

  nav.classList.remove("nav-ready");
  if (!authenticated) {
    links.push({ href: "/login.html", text: "Giriş Yap" });
  } else if (roles.includes("Admin")) {
    links.push({ href: "/admin.html", text: "Admin" });
    links.push({ href: "/verify.html", text: "Doğrulama" });
  } else if (roles.includes("University")) {
    links.push({ href: "/index.html", text: "Kayıt" });
    links.push({ href: "/university-students.html", text: "Öğrenci Yönetimi" });
    links.push({ href: "/verify.html", text: "Doğrulama" });
  } else if (roles.includes("Student")) {
    links.push({ href: "/student.html", text: "Öğrenci" });
  } else if (roles.includes("Employer")) {
    links.push({ href: "/verify.html", text: "Doğrulama" });
  }

  nav.innerHTML = "";
  for (const link of links) {
    const anchor = document.createElement("a");
    anchor.href = link.href;
    anchor.textContent = link.text;
    if (isCurrentPath(link.href)) {
      anchor.className = "active";
    }
    nav.appendChild(anchor);
  }

  if (authenticated) {
    const account = document.createElement("span");
    account.className = "account-pill";
    account.textContent = session.email || "Hesap";
    nav.appendChild(account);

    const logout = document.createElement("button");
    logout.id = "logoutButton";
    logout.className = "nav-logout";
    logout.type = "button";
    logout.textContent = "Çıkış Yap";
    nav.appendChild(logout);
  }

  nav.classList.add("nav-ready");
}

function isCurrentPath(href) {
  const path = window.location.pathname;

  if (href === "/index.html") {
    return path.endsWith("/index.html");
  }

  return path.endsWith(href);
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

function wireFormResets() {
  document.addEventListener("reset", (event) => {
    const form = event.target;

    setTimeout(() => {
      if (form.id === "uploadForm") {
        setText("#uploadFileName", "PDF seç veya buraya sürükle");
        const studentSelect = document.querySelector("#studentAccountSelect");
        if (studentSelect) {
          studentSelect.value = "";
        }
        hide(document.querySelector("#uploadResult"));
        hide(document.querySelector("#uploadMessage"));
      }

      if (form.id === "verifyForm") {
        setText("#verifyFileName", "PDF seç veya buraya sürükle");
        hide(document.querySelector("#verifyResult"));
        hide(document.querySelector("#verifyMessage"));
      }

      if (form.id === "userForm") {
        updateUserRoleFields();
      }
    }, 0);
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
    if (isBusy) {
      button.dataset.originalText = button.dataset.originalText || button.textContent;
      button.textContent = button.textContent.includes("Doğrula")
        ? "Doğrulanıyor..."
        : "Kaydediliyor...";
    } else if (button.dataset.originalText) {
      button.textContent = button.dataset.originalText;
      delete button.dataset.originalText;
    }
  }
}

function showLoadingMessage(element, text) {
  if (!element) {
    return;
  }

  element.innerHTML = `<span class="spinner" aria-hidden="true"></span><span>${escapeHtml(text)}</span>`;
  element.className = "message loading";
  show(element);
}

function showMessage(element, text, isError) {
  if (!element) {
    return;
  }

  element.textContent = text;
  element.className = isError ? "message error" : "message";
  show(element);
}

function setText(selector, text) {
  const element = document.querySelector(selector);
  if (element) {
    element.textContent = text;
  }
}

function setTextInputValue(selector, value) {
  const input = document.querySelector(selector);
  if (input) {
    input.value = value;
    input.dispatchEvent(new Event("input", { bubbles: true }));
  }
}

function toggleTrustBadge(selector, status, signatureValid) {
  const badge = document.querySelector(selector);
  if (!badge) {
    return;
  }

  if (status === "Geçerli Diploma" && signatureValid) {
    show(badge);
  } else {
    hide(badge);
  }
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}
