document.addEventListener("DOMContentLoaded", () => {
  const tasks = document.querySelectorAll(".task[time]");
  let total = 0;

  tasks.forEach(task => {
    const timeStr = task.getAttribute("time");
    let hours = 0;

    if (timeStr.includes("h")) hours += parseFloat(timeStr);
    if (timeStr.includes("m")) hours += parseFloat(timeStr) / 60;

    // show aligned value
    const span = document.createElement("span");
    span.className = "task-time";
    span.textContent = timeStr;
    task.appendChild(span);

    total += hours;
  });

  const totalDiv = document.querySelector(".task-total");
  if (totalDiv) {
    totalDiv.textContent = `Total time spent: ${total.toFixed(2)}h`;
  }
});
