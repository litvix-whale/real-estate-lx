using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Models;

namespace Logic.Services
{
    public class RealEstateService(IRealEstateRepository realEstateRepository) : IRealEstateService
    {
        private readonly IRealEstateRepository _realEstateRepository = realEstateRepository;

        public async Task<IEnumerable<RealEstate>> SearchRealEstateAsync(RealEstateSearchCriteria criteria)
        {
            return await _realEstateRepository.SearchAsync(criteria);
        }

        public async Task<int> GetSearchCountAsync(RealEstateSearchCriteria criteria)
        {
            return await _realEstateRepository.GetSearchCountAsync(criteria);
        }

        public async Task<RealEstate?> GetRealEstateByIdAsync(Guid id)
        {
            return await _realEstateRepository.GetByIdAsync(id);
        }

        public async Task<RealEstate?> GetRealEstateWithImagesAsync(Guid id)
        {
            return await _realEstateRepository.GetByIdWithImagesAsync(id);
        }

        public async Task<string> CreateRealEstateAsync(RealEstate realEstate)
        {
            try
            {
                realEstate.CreatedAt = DateTime.UtcNow;
                await _realEstateRepository.AddAsync(realEstate);
                return "Success";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<string> UpdateRealEstateAsync(RealEstate realEstate)
        {
            try
            {
                realEstate.UpdatedAt = DateTime.UtcNow;
                await _realEstateRepository.UpdateAsync(realEstate);
                return "Success";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<string> DeleteRealEstateAsync(Guid id)
        {
            try
            {
                await _realEstateRepository.DeleteAsync(id);
                return "Success";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<IEnumerable<RealEstate>> GetUserRealEstatesAsync(Guid userId)
        {
            return await _realEstateRepository.GetByUserIdAsync(userId);
        }
    }
}