using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LMS.BLL.Interfaces;
using LMS.Core.Models;

namespace LMS.BLL.Services
{
    public class LanguageService : ILanguageService
    {
        private readonly ILanguageRepository _languageRepository;

        public LanguageService(ILanguageRepository languageRepository)
        {
            _languageRepository = languageRepository;
        }

        public async Task<IEnumerable<Language>> GetLanguagesAsync(bool onlyApproved = true)
        {
            var languages = await _languageRepository.GetAllAsync();
            if (onlyApproved)
            {
                return languages.Where(l => l.IsApproved).ToList();
            }
            return languages;
        }

        public async Task<Language?> GetLanguageByIdAsync(int id)
        {
            return await _languageRepository.Get(id);
        }

        public async Task<Language> CreateLanguageAsync(string name, bool isApproved)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Language name cannot be empty.");
            }
            name = name.Trim();

            var all = await _languageRepository.GetAllAsync();
            if (all.Any(l => l.Name.Trim().Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException($"Language '{name}' already exists.");
            }

            var language = new Language
            {
                Name = name,
                IsApproved = isApproved,
                CreatedAt = DateTime.UtcNow
            };
            return await _languageRepository.Create(language);
        }

        public async Task<bool> UpdateLanguageAsync(int id, string name)
        {
            var language = await _languageRepository.Get(id);
            if (language == null) return false;

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Language name cannot be empty.");
            }
            name = name.Trim();

            var all = await _languageRepository.GetAllAsync();
            if (all.Any(l => l.Id != id && l.Name.Trim().Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException($"Language '{name}' already exists.");
            }

            language.Name = name;
            await _languageRepository.Update(language);
            return true;
        }

        public async Task<bool> DeleteLanguageAsync(int id)
        {
            var language = await _languageRepository.Get(id);
            if (language == null) return false;

            await _languageRepository.Delete(language);
            return true;
        }

        public async Task<bool> ApproveLanguageAsync(int id)
        {
            var language = await _languageRepository.Get(id);
            if (language == null) return false;

            language.IsApproved = true;
            await _languageRepository.Update(language);
            return true;
        }
    }
}
