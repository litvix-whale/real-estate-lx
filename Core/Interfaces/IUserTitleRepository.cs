using Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Interfaces;

public interface IUserTitleRepository
{
    Task<UserTitle?> GetByUserIdAndTopicIdAsync(Guid userId, Guid topicId);
    Task<List<UserTitle>> GetByUserIdAsync(Guid userId);
    Task AddAsync(UserTitle userTitle);
    Task DeleteAsync(Guid id);
    Task<bool> UserHasTitleAsync(Guid userId, Guid topicId);
}