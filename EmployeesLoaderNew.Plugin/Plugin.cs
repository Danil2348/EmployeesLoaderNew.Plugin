using Newtonsoft.Json.Linq;
using PhoneApp.Domain.Attributes;
using PhoneApp.Domain.DTO;
using PhoneApp.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace LoaderNewEmployees
{
    /// <summary>
    /// Плагин для загрузки сотрудников из внешнего API.
    /// </summary>
    [Author(Name = "Ovchinnikov Danil")]
    public class Plugin : IPluggable
    {
        #region Поля и Константы
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private const string URL_ADRESS = "https://dummyjson.com/users";
        #endregion

        #region Основной метод запуска

        /// <summary>
        /// Запускает процесс загрузки сотрудников.
        /// </summary>
        /// <param name="args">Входные данные (не используются).</param>
        /// <returns>Коллекция загруженных сотрудников в виде DataTransferObject.</returns>
        public IEnumerable<DataTransferObject> Run(IEnumerable<DataTransferObject> args)
        {
            try
            {
                logger.Info("Loading employees");

                var employeesList = GetUsersFromApi(URL_ADRESS);

                logger.Info($"Loaded {employeesList.Count} employees");

                return employeesList.Cast<DataTransferObject>();
            }

            catch (Exception ex)
            {
                logger.Error(ex, "Ошибка при загрузке сотрудников");
                throw;
            }
        }
        #endregion

        #region Вспомогательные методы

        /// <summary>
        /// Получает список сотрудников из API по указанному URL.
        /// </summary>
        /// <param name="url">URL API для загрузки пользователей.</param>
        /// <returns>Список сотрудников в виде объектов EmployeesDTO.</returns>
        private List<EmployeesDTO> GetUsersFromApi(string url)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            using (var client = new WebClient())
            {
                try
                {
                    var jArray = GetJArray(url, client);

                    var users = jArray
                        .Select(item =>
                        {
                            var firstName = (string)item["firstName"] ?? string.Empty;
                            var lastName = (string)item["lastName"] ?? string.Empty;
                            var phone = (string)item["phone"] ?? string.Empty;

                            var user = new EmployeesDTO
                            {
                                Name = $"{firstName} {lastName}".Trim()
                            };
                            user.AddPhone(phone);

                            return user;
                        })
                        .ToList();

                    return users;
                }

                catch (Exception ex)
                {
                    logger.Error(ex, "Ошибка при обработке списка пользователей из JSON.");
                    return new List<EmployeesDTO>();
                }
            }
        }

        /// <summary>
        /// Загружает JSON с указанного URL и возвращает массив пользователей.
        /// </summary>
        /// <param name="url">URL для загрузки JSON.</param>
        /// <param name="client">Экземпляр WebClient для загрузки данных.</param>
        /// <returns>JArray с пользователями.</returns>
        /// <exception cref="Exception">Если JSON не содержит массив 'users' или произошла ошибка загрузки/парсинга.</exception>
        private JArray GetJArray(string url, WebClient client)
        {
            try
            {
                var json = client.DownloadString(url);

                var jObject = JObject.Parse(json);
                var jArray = jObject["users"] as JArray;

                if (jArray == null)
                    throw new Exception("В JSON отсутствует массив 'users' или он имеет неверный формат.");

                return jArray;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Ошибка при получении массива 'users' из JSON");
                throw new Exception("Ошибка при загрузке или парсинге JSON с URL: " + url, ex);
            }
        }
        #endregion
    }
}