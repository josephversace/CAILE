using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    public interface IConfigurationService
    {
        Task<T> GetSettingAsync<T>(string key, T defaultValue = default);
        Task SetSettingAsync<T>(string key, T value);
        Task<Dictionary<string, object>> GetAllSettingsAsync();
        Task ReloadSettingsAsync();
    }

}
