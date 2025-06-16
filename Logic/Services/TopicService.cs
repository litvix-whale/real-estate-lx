using System.Diagnostics.CodeAnalysis;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Repositories;

namespace Logic.Services
{
    public class TopicService(ITopicRepository topicRepository, IUserTopicRepository userTopicRepository, ITopicCategoryRepository topicCategoryRepository) : ITopicService
    {
        private const string SuccessMessage = "Success";
        private readonly ITopicRepository _topicRepository = topicRepository;
        private readonly IUserTopicRepository _userTopicRepository = userTopicRepository;
        private readonly ITopicCategoryRepository _topicCategoryRepository = topicCategoryRepository;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<string> SubscribeUserAsync(string email, string topicName)
        {
            try
            {
                await _userTopicRepository.AddAsync(email, topicName);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return SuccessMessage;
        }
        public async Task<string> UnsubscribeUserAsync(string email, string topicName)
        {
            try
            {
                await _userTopicRepository.DeleteAsync(email, topicName);
            }
            catch (Exception)
            {
                return "Failed to unsubscribe user.";
            }
            return SuccessMessage;
        }
        public async Task<bool> IsUserSubscribedAsync(string email, string topicName)
        {
            return await _userTopicRepository.IsUserSubscribedAsync(email, topicName);
        }
        public async Task<IEnumerable<Topic>> GetTopicsByNameAsync(string name)
        {
            return await _topicRepository.GetTopicsByNameAsync(name);
        }
        public async Task<Topic> GetByNameAsync(string name)
        {
            return await _topicRepository.GetByNameAsync(name);
        }
        public async Task<Topic> GetByIdAsync(Guid id)
        {
            return await _topicRepository.GetByIdAsync(id) ?? new Topic();
        }
        public async Task<IEnumerable<Topic>> GetAllTopicsAsync()
        {
            return await _topicRepository.GetAllTopicsAsync();
        }
        public async Task<IEnumerable<Topic>> GetTopics(int start, int count, string searchQuery)
        {
            return await _topicRepository.GetTopics(start, count, searchQuery);
        }
        public async Task<string> CreateTopicAsync(string topicName, string categoryName = "")
        {
            try
            {
                await _topicRepository.AddAsync(new Topic { Name = topicName });
                await _topicCategoryRepository.AddAsync(topicName, categoryName);
            }
            catch (Exception)
            {
                return "Failed to create topic.";
            }
            return SuccessMessage;
        }
        public async Task<string> DeleteTopicAsync(string name)
        {
            var topic = await _topicRepository.GetByNameAsync(name);
            if (topic == null)
            {
                return "Topic not found.";
            }
            try
            {
                await _topicRepository.DeleteAsync(topic.Id);
            }
            catch (Exception)
            {
                return "Failed to delete topic.";
            }
            return SuccessMessage;
        }
        public async Task<string> FilterTopicsByCategoryAsync(string category)
        {
            throw new NotImplementedException();
        }
        public async Task<IEnumerable<User>> GetTopicSubscribersAsync(string topicName)
        {
            return await _userTopicRepository.GetTopicSubscribersAsync(topicName);
        }
    }
}
