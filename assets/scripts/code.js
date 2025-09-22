async function readFile(filePath) {
  try {
    const response = await fetch(filePath);
    if (!response.ok) throw new Error("Network error");
    return await response.text();
  } catch (err) {
    console.error("Fetch error:", err);
  }
}

function renderCode(element, content, filePath) {
  const fileName = filePath.split("/").pop();
  element.innerHTML = `
    <div class="code-container">
      <p class="code-file"><a href="${filePath}">Download ${fileName}</a></p>
      <pre class="language-${element.getAttribute("lang") || "cs"}"><code class="language-${element.getAttribute("lang") || "cs"}">${Prism.highlight(content, Prism.languages[element.getAttribute("lang") || "csharp"], element.getAttribute("lang") || "csharp")}</code></pre>
    </div>
  `;
}

document.addEventListener("DOMContentLoaded", async () => {
  const codeElements = Array.from(document.querySelectorAll(".code"));
  for (const el of codeElements) {
    const path = el.getAttribute("href");
    const content = await readFile(path);
    if (content) renderCode(el, content, path);
  }
});