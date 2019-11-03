using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Data
{
    public class DatingRepository:IDatingRepository
    {
        private DataContext _context;

        public DatingRepository(DataContext context)
        {
            _context = context;
        }

        public void Add<T>(T entity) where T : class
        {
            _context.Add(entity);
        }

        public void Delete<T>(T entity) where T : class
        {
            _context.Remove(entity);
        }

        public async Task<bool> SaveAll()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<PagedList<User>> GetUsers(UserParams userParams)
        {
            var users = _context.Users.Include(p => p.Photos).OrderByDescending(u=>u.LastActive).AsQueryable();

            users = users.Where(u => u.Id != userParams.UserId);
            users = users.Where(u => u.Gender == userParams.Gender);

            if (userParams.Likers)
            {
                var userLikers = await GetUserLikes(userParams.UserId, userParams.Likers);
                users = users.Where(u => userLikers.Contains(u.Id));
            }

            if (userParams.Likees)
            {
                var userLikees = await GetUserLikes(userParams.UserId, userParams.Likers);
                users = users.Where(u => userLikees.Contains(u.Id));
            }
            if (userParams.MinAge!=18 || userParams.MaxAge!=99)
            {
                var minDob = DateTime.Today.AddYears(-userParams.MaxAge-1);
                var maxDob = DateTime.Today.AddYears(-userParams.MinAge);
                users = users.Where(u => u.DateOfBirth >= minDob && u.DateOfBirth <= maxDob);
            }

            if (!string.IsNullOrEmpty(userParams.OrderBy))
            {
                switch (userParams.OrderBy)
                {
                    case "created":
                        users = users.OrderByDescending(u => u.Created);
                        break;
                    default:
                        users = users.OrderByDescending(u => u.LastActive);
                        break;
                        
                }
                
            }
            return await PagedList<User>.CrateAsync(users, userParams.PageNumber, userParams.PageSize);
        }

        private async Task<IEnumerable<int>> GetUserLikes(int id, bool likers)
        {
            var user = await _context.Users.Include(x => x.Likees).Include(x => x.Likers)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (likers)
            {
                return user.Likers.Where(u => u.LikeeId == id).Select(i => i.LikerId);
            }
            else
            {
                return user.Likees.Where(u => u.LikerId == id).Select(i => i.LikeeId);
            }
        }

        //public async Task<IEnumerable<User>> GetUsers()
        //{

        //    var users =await _context.Users.Include(p => p.Photos)
        //        .ToListAsync();
        //    return users;
        //}

        public async Task<User> GetUser(int userId)
        {
            var user = await _context.Users.Include(x => x.Photos)
                .FirstOrDefaultAsync(u => u.Id == userId);
            return user;
        }

        public async Task<Photo> GetPhoto(int id)
        {
            var photo = await _context.Photos.FirstOrDefaultAsync(p => p.Id == id);
            return photo;
        }

        public async Task<User> GetMe(int id)
        {
            var user = await _context.Users.Include(p => p.Photos).IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id);
            return user;
        }

        public async Task<Photo> GetMainPhoto(int id)
        {
            var photo = await _context.Photos.Where(i => i.UserId == id).FirstOrDefaultAsync(m => m.IsMain);
            return photo;
        }

        public async Task<Like> GetLike(int userId, int recipientId)
        {
            return await _context.Likes.FirstOrDefaultAsync(u => u.LikerId == userId && u.LikeeId == recipientId);
        }

        public async Task<Message> GetMessage(int id)
        {
            return await _context.Messages.FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<PagedList<Message>> GetMessagesForUser(MessageParams  messageParams)
        {
            var messages = _context.Messages.Include(u => u.Sender).ThenInclude(p => p.Photos).Include(u => u.Recipient).ThenInclude(p => p.Photos).AsQueryable();
            switch (messageParams.MessageContainer)
            {
                case "Inbox":
                    messages = messages.Where(u => u.RecipientId == messageParams.UserId && u.IsDeletedByRecipient == false);
                    break;
                case "Outbox":
                    messages = messages.Where(u => u.SenderId == messageParams.UserId && u.IsDeletedBySender == false);
                    break;
                default:
                    messages = messages.Where(u => u.RecipientId == messageParams.UserId && u.IsDeletedByRecipient == false && u.IsRead == false);
                    break;
            }
            messages = messages.OrderByDescending(d => d.SendDate);
            return await PagedList<Message>.CrateAsync(messages, messageParams.PageNumber, messageParams.PageSize);
        }

        public async Task<IEnumerable<Message>> GetMessageThread(int userId, int recipientId)
        {
            var messages = await _context.Messages.Include(u => u.Sender).ThenInclude(p => p.Photos).Include(u => u.Recipient).ThenInclude(p => p.Photos)
                .Where(m => (m.RecipientId == userId && m.IsDeletedByRecipient == false && m.SenderId == recipientId) || (m.SenderId == userId && m.IsDeletedBySender == false && m.RecipientId == recipientId))
                .OrderByDescending(m => m.SendDate).ToListAsync();
            return messages;
        }
    }
}
