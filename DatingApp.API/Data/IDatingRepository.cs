﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatingApp.API.Helpers;
using DatingApp.API.Models;

namespace DatingApp.API.Data
{
    public interface IDatingRepository
    {
        void Add<T>(T entity) where T : class;
        void Delete<T>(T entity) where T : class;
        Task<bool> SaveAll();
      //  Task<IEnumerable<User>> GetUsers();
       Task<PagedList<User>> GetUsers(UserParams userParams);
        Task<User> GetUser(int userId);
        Task<Photo> GetPhoto(int id);
        Task<User> GetMe(int id);
        Task<Photo> GetMainPhoto(int id);
        Task<Like> GetLike(int userId, int recipientId);
        Task<Message> GetMessage(int id);
        Task<PagedList<Message>> GetMessagesForUser(MessageParams  messageParams);
        Task<IEnumerable<Message>> GetMessageThread(int userId, int recipientId);
    }
}
