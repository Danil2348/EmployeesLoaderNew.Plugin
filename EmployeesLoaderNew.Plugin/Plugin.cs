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
        /// Загружает сотрудников из внешнего API и добавляет их к существующим данным.
        /// Если загрузка не удалась, возвращает исходные данные без изменений.
        /// </summary>
        /// <param name="args">Исходная коллекция сотрудников.</param>
        /// <returns>Объединённая коллекция сотрудников.</returns>
        public IEnumerable<DataTransferObject> Run(IEnumerable<DataTransferObject> args)
        {
            logger.Info("Loading employees");

            var employeesList = GetUsersFromApi(URL_ADRESS);

            logger.Info($"Loaded {employeesList.Count} employees");

            var newArgs = args.Concat(employeesList.Cast<DataTransferObject>());

            return newArgs;
        }
        #endregion

        #region Вспомогательные методы

        /// <summary>
        /// Загружает сотрудников из внешнего API и добавляет их к существующим данным.
        /// Если загрузка не удалась, возвращает исходные данные без изменений.
        /// </summary>
        /// <param name="args">Исходная коллекция сотрудников.</param>
        /// <returns>Объединённая коллекция сотрудников.</returns>
        private List<EmployeesDTO> GetUsersFromApi(string url)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var users = new List<EmployeesDTO>();

            using (var client = new WebClient())
            {
                try
                {
                    var jArray = GetJArray(url, client);

                    users = jArray
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
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Ошибка при обработке списка пользователей из JSON.");
                }

                return users;
            }
        }

        /// <summary>
        /// Загружает JSON с указанного URL и возвращает массив пользователей.
        /// Если массив 'users' отсутствует или JSON некорректен, возвращает пустой массив и логирует предупреждение.
        /// </summary>
        /// <param name="url">URL для загрузки JSON.</param>
        /// <param name="client">WebClient для загрузки данных.</param>
        /// <returns>Массив пользователей или пустой массив при ошибках.</returns>
        private JArray GetJArray(string url, WebClient client)
        {
            var jArray = new JArray();

            try
            {
                var json = client.DownloadString(url);

                var jObject = JObject.Parse(json);
                jArray = jObject["users"] as JArray;

                if (jArray == null)
                    logger.Warn("В JSON отсутствует массив 'users' или он имеет неверный формат.");

                return jArray;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Ошибка при получении массива 'users' из JSON");
            }

            return jArray;
        }
        #endregion
    }
}