using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    /// <summary>
    /// Interface for managing local UI preferences and settings.
    /// </summary>
    public interface ILocalSettingsService
    {
        /// <summary>
        /// Retrieves a setting value by key.
        /// </summary>
        /// <typeparam name="T">Type of the setting value</typeparam>
        /// <param name="key">Setting key</param>
        /// <returns>Setting value or default if not found</returns>
        Task<T?> GetAsync<T>(string key);

        /// <summary>
        /// Stores a setting value.
        /// </summary>
        /// <typeparam name="T">Type of the setting value</typeparam>
        /// <param name="key">Setting key</param>
        /// <param name="value">Setting value to store</param>
        Task SetAsync<T>(string key, T value);

        /// <summary>
        /// Removes a setting by key.
        /// </summary>
        /// <param name="key">Setting key to remove</param>
        Task RemoveAsync(string key);
    }
}
