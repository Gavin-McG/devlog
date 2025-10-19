using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manager for managing tasks of the Task System
/// </summary>
public partial class TaskManager : MonoBehaviour
{
    private HashSet<TaskSystem> startedTaskSystems = new();
    
    private HashSet<TaskSO> completedTasks = new();
    private HashSet<string> completedTaskNames = new();
    
    private List<ActiveTask> activeTasks = new();
    private Dictionary<TaskSO, ActiveTask> taskDict = new();
    
    private void OnDestroy()
    {
        ClearAllTasks();
    }
    
    /// <summary>
    /// Begin a task by inserting at a specific position in the task list
    /// </summary>
    private bool BeginTask(TaskSO task, int index)
    {
        // Create and register active task
        ActiveTask activeTask = new(task);
        activeTasks.Insert(index, activeTask);
        taskDict.Add(task, activeTask);

        // Listen for task completion
        activeTask.OnTaskCompleted.AddListener(() => CompleteTask(activeTask.task));

        // Listen for individual requirement completions
        activeTask.OnRequirementCompleted.AddListener((requirementIndex) =>
        {
            OnRequirementCompleted.Invoke(new RequirementEventData(index, requirementIndex, task));
        });

        // Notify UI that a task was added
        OnTaskAdded.Invoke(new TaskEventData(index, task));

        return true;
    }

    /// <summary>
    /// Begin a task. Cannot start a task that is already ongoing or completed
    /// </summary>
    public bool BeginTask(TaskSystem taskSystem, bool clearExisting = false)
    {
        // Optionally clear active tasks belonging to this system
        if (clearExisting)
        {
            ClearTasksFromSystem(taskSystem);
        }
        
        if (startedTaskSystems.Contains(taskSystem))
        {
            Debug.LogWarning("Attempting to start task system that has been started before");
            return false;
        }

        // Start the system's initial tasks
        taskSystem.initialTasks.ForEach(task => BeginTask(task, activeTasks.Count));
        startedTaskSystems.Add(taskSystem);
        return true;
    }

    /// <summary>
    /// End a currently Active Task
    /// </summary>
    public bool EndTask(TaskSO task)
    {
        if (!taskDict.TryGetValue(task, out ActiveTask activeTask))
        {
            Debug.LogWarning("Attempting to end task that is not active");
        }
        
        int index = activeTasks.IndexOf(activeTask);
        activeTasks.RemoveAt(index);
        taskDict.Remove(task);
        
        activeTask.ClearListeners();
        OnTaskRemoved.Invoke(new TaskEventData(index, task));
        
        return true;
    }
    
    /// <summary>
    /// Mark task as completed for manual override
    /// (Also used for internal purposes)
    /// </summary>
    public bool CompleteTask(TaskSO task)
    {
        if (!taskDict.TryGetValue(task, out var activeTask))
        {
            Debug.LogWarning("Attempting to complete task that has not begun");
            return false;
        }

        int index = activeTasks.IndexOf(activeTask);
        taskDict.Remove(task);
        activeTasks.RemoveAt(index);
        activeTask.ClearListeners();

        completedTasks.Add(task);
        completedTaskNames.Add(task.name);

        // Fire event for UI
        OnTaskCompleted.Invoke(new TaskEventData(index, task));

        // Add next tasks in reverse order
        var nextTasks = task.nextTasks;
        nextTasks.Reverse();
        foreach (var nextTask in nextTasks)
        {
            //skip next task if already finished or active
            if (IsCompleted(nextTask) || IsActive(nextTask)) continue;
            
            if (!nextTask.requirePrevious || nextTask.previousTasks.Aggregate(true, (current, prevTask) => current && IsCompleted(prevTask)))
            {
                // Begin next if previous tasks aren't required or is all previous tasks are complete
                BeginTask(nextTask, index);
            }
        }

        return true;
    }
    
    private void CollectAllTasksRecursive(TaskSO task, HashSet<TaskSO> collected)
    {
        if (task == null || !collected.Add(task))
            return;

        foreach (var next in task.nextTasks)
            CollectAllTasksRecursive(next, collected);
    }

    /// <summary>
    /// Clears all active tasks and forgets all completed tasks
    /// </summary>
    public void ClearAllTasks()
    {
        // Clear listeners
        while (activeTasks.Count > 0)
        {
            EndTask(activeTasks[0].task);
        }

        // Clear lists
        startedTaskSystems.Clear();
        completedTaskNames.Clear();
        activeTasks.Clear();
        taskDict.Clear();
    }
    
    /// <summary>
    /// Clears all active tasks that belong to the given task system
    /// </summary>
    private void ClearTasksFromSystem(TaskSystem taskSystem)
    {
        // Collect all tasks within the system recursively
        HashSet<TaskSO> allTasksInSystem = new();
        foreach (var initial in taskSystem.initialTasks)
            CollectAllTasksRecursive(initial, allTasksInSystem);

        // Find and remove matching active tasks
        var tasksToRemove = activeTasks
            .Where(active => allTasksInSystem.Contains(active.task))
            .ToList();

        foreach (var activeTask in tasksToRemove)
        {
            EndTask(activeTask.task);
        }
        
        startedTaskSystems.Remove(taskSystem);

        Debug.Log($"Cleared {tasksToRemove.Count} active tasks from system '{taskSystem.name}'.");
    }
    
    /// <summary>
    /// Check if all tasks are complete in task System
    /// </summary>
    public bool IsCompleted(TaskSystem taskSystem) => 
        taskSystem.initialTasks.All(task => IsCompletedRecursive(task));
    
    /// <summary>
    /// Check if task has been completed by taskSO
    /// </summary>
    public bool IsCompleted(TaskSO task) => completedTasks.Contains(task);
    
    /// <summary>
    /// Check if task has been completed by name
    /// </summary>
    public bool IsCompleted(string taskName) => completedTaskNames.Contains(taskName);

    /// <summary>
    /// Check if a task and all its subsequent tasks have been completed
    /// </summary>
    public bool IsCompletedRecursive(TaskSO task)
    {
        return IsCompletedRecursive(task, new HashSet<TaskSO>());
    }

    private bool IsCompletedRecursive(TaskSO task, HashSet<TaskSO> visited)
    {
        if (task == null) return true;

        // Cycle detected — stop infinite recursion
        if (!visited.Add(task))
        {
            Debug.LogWarning($"Cycle detected in task graph at {task.name}");
            return true; // or false depending on desired behavior
        }

        bool hasCompleted = IsCompleted(task);
        foreach (var nextTask in task.nextTasks)
            hasCompleted = hasCompleted && IsCompletedRecursive(nextTask, visited);

        return hasCompleted;
    }

    /// <summary>
    /// Check if any task in ongoing in task System
    /// </summary>
    public bool IsActive(TaskSystem taskSystem) => 
        taskSystem.initialTasks.Any(task => IsActiveRecursive(task));
    
    /// <summary>
    /// Check if task in ongoing by taskSO
    /// </summary>
    public bool IsActive(TaskSO task) => taskDict.ContainsKey(task);
    
    /// <summary>
    /// Check if task in ongoing by name
    /// </summary>
    public bool IsActive(string taskName) => taskDict.Keys.Any(task => task.name == taskName);

    /// <summary>
    /// Check if a task or any of its subsequent tasks are ongoing
    /// </summary>
    public bool IsActiveRecursive(TaskSO task)
    {
        return IsActiveRecursive(task, new HashSet<TaskSO>());
    }

    private bool IsActiveRecursive(TaskSO task, HashSet<TaskSO> visited)
    {
        if (task == null) return false;

        if (!visited.Add(task))
        {
            Debug.LogWarning($"Cycle detected in task graph at {task.name}");
            return false;
        }

        bool isActive = IsActive(task);
        foreach (var nextTask in task.nextTasks)
            isActive = isActive || IsActiveRecursive(nextTask, visited);

        return isActive;
    }
}
