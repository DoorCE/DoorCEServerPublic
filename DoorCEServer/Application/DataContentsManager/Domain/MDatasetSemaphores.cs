namespace DoorCEServer.Application.DataContentsManager.Domain;

public static class MDatasetSemaphores
{
    // Semaphore per dataset with automatic cleanup
    private class SemaphoreEntry
    {
        public readonly SemaphoreSlim Semaphore = new(1, 1);
        public int WaiterCount; // Number of threads waiting or holding the semaphore
    }

    private static readonly Lock Lock = new();
    private static readonly Dictionary<string, SemaphoreEntry> Semaphores = new();

    public static void Wait(string datasetUri)
    {
        SemaphoreEntry entry;
        lock (Lock) {
            if (!Semaphores.TryGetValue(datasetUri, out entry!)) // get existing or create a new semaphore
                Semaphores[datasetUri] = entry = new SemaphoreEntry();
            entry.WaiterCount++; // increase the waiting thread counter
        }
        entry.Semaphore.Wait(); // wait outside of lock - precents from deadlock
    }

    public static async Task WaitAsync(string datasetUri)
    {
        SemaphoreEntry entry;
        lock (Lock) {
            if (!Semaphores.TryGetValue(datasetUri, out entry!))
                Semaphores[datasetUri] = entry = new SemaphoreEntry();
            entry.WaiterCount++;
        }
        await entry.Semaphore.WaitAsync();
    }

    public static void Release(string datasetUri)
    {
        lock (Lock) {
            if (!Semaphores.TryGetValue(datasetUri, out SemaphoreEntry? entry))
                return;
            entry.Semaphore.Release(); // safely release inside a lock
            if (--entry.WaiterCount == 0) {
                Semaphores.Remove(datasetUri);
                entry.Semaphore.Dispose(); // dispose of the semaphore when no thread is waiting
            }
        }
    }
}
