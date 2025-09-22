document.addEventListener("DOMContentLoaded", () => {
  const tasks = document.querySelectorAll(".task[time]");
  let total = 0;

  tasks.forEach(task => {
    const timeStr = task.getAttribute("time");
    let hours = 0;

    if (timeStr.includes("h")) hours += parseFloat(timeStr);
    if (timeStr.includes("m")) hours += parseFloat(timeStr) / 60;

    // Grab the original inner text of the task (before we restructure it)
    const taskName = task.textContent.trim();

    // Clear existing content
    task.innerHTML = "";

    // Create header wrapper
    const headerDiv = document.createElement("div");
    headerDiv.className = "task-heading";

    // Task name as <h1>
    const titleEl = document.createElement("h1");
    titleEl.textContent = taskName;

    // Time as <p>
    const timeEl = document.createElement("strong");
    timeEl.className = "task-time";
    timeEl.textContent = `(${timeStr})`;

    // Assemble
    headerDiv.appendChild(titleEl);
    headerDiv.appendChild(timeEl);
    task.appendChild(headerDiv);

    total += hours;
  });

  const totalDiv = document.querySelector(".task-total")
    .querySelector(".task-time");
  if (totalDiv) {
    totalDiv.textContent = `${total.toFixed(1)}h`;
  }
});
