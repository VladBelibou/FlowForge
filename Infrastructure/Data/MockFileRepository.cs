using System.Text.Json;

namespace ManufacturingScheduler.Infrastructure.Data
{
    public class MockFileRepository<T> where T : class
    {
        private readonly string _filePath;

        public MockFileRepository(string fileName)
        {
            _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MockData", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

            Console.WriteLine($"DEBUG: Looking for file at: {_filePath}");
            Console.WriteLine($"DEBUG: File exists: {File.Exists(_filePath)}");

            if (!File.Exists(_filePath))
            {
                Console.WriteLine($"DEBUG: Creating empty file: {_filePath}");
                File.WriteAllText(_filePath, "[]");
            }
        }

        public async Task<List<T>> GetAllAsync()
        {
            try
            {
                var json = await File.ReadAllTextAsync(_filePath);
                Console.WriteLine($"DEBUG: File content length: {json.Length}");
                Console.WriteLine($"DEBUG: First 200 chars: {json.Substring(0, Math.Min(200, json.Length))}");

                var result = JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
                Console.WriteLine($"DEBUG: Deserialized {result.Count} items of type {typeof(T).Name}");

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR reading mock data: {ex.Message}");
                Console.WriteLine($"ERROR stack trace: {ex.StackTrace}");
                return new List<T>();
            }
        }

        public async Task<T?> GetByIdAsync<TId>(TId id, Func<T, TId, bool> idComparer)
        {
            var items = await GetAllAsync();
            return items.FirstOrDefault(item => idComparer(item, id));
        }

        public async Task SaveAsync(List<T> items)
        {
            try
            {
                var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_filePath, json);
                Console.WriteLine($"DEBUG: Saved {items.Count} items to {_filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR saving mock data: {ex.Message}");
                throw;
            }
        }

        public async Task SaveItemAsync(T item, Func<T, object> idSelector)
        {
            var items = await GetAllAsync();
            var existingItem = items.FirstOrDefault(x =>
                idSelector(x).Equals(idSelector(item)));

            if (existingItem != null)
            {
                items.Remove(existingItem);
            }

            items.Add(item);
            await SaveAsync(items);
        }

        public async Task DeleteItemAsync<TId>(TId id)
        {
            var items = await GetAllAsync();
            var itemToRemove = items.FirstOrDefault(item =>
            {
                var idProperty = typeof(T).GetProperty("Id");
                if (idProperty != null)
                {
                    var itemId = idProperty.GetValue(item);
                    return itemId?.Equals(id) == true;
                }
                return false;
            });

            if (itemToRemove != null)
            {
                items.Remove(itemToRemove);
                await SaveAsync(items);
            }
        }
    }
}
