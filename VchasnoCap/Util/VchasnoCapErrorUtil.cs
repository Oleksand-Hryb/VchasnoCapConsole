namespace VchasnoCapConsole.VchasnoCap.Util
{
    internal static class VchasnoCapErrorUtil
    {
        public static string GetErrorCodeMessage(int? errorCode)
        {
            switch (errorCode)
            {
                case null:
                case 0: return "виконано успішно";
                case 1: return "невірний формат";
                case 2: return "невірний параметр";
                case 3: return "реквізити сертифікатів не зазначено";
                case 4: return "невідповідні реквізити сертифікатів";
                case 5: return "внутрішній та зовнішній ід.клієнта не співпадають";
                case 6: return "вже зареєстровано";
                case 7: return "не зареєстровано";
                case 8: return "не знайдено клієнта з таким ідентифікатором";
                case 9: return "не знайдено сервер прикладної системи з таким ідентифікатором";
                case 11: return "невірний сертифікат";
                case 12: return "відсутні сертифікати (не подані)";
                case 13: return "відсутній сертифікат ПРК";
                case 14: return "відсутній сертифікат підпису";
                case 15: return "помилка при розшифруванні запиту";
                case 16: return "помилка при перевірці підпису запиту";
                case 17: return "відсутній ідентифікатор клієнта";
                case 18: return "невірний ідентифікатор клієнта";
                case 19: return "невірний ідентифікатор операції";
                case 20: return "повторний запит";
                case 21: return "операція не підтримується";
                case 22: return "операцію зареєстровано";
                case 28: return "внутрішня помилка сервера підпису";
            }
            return "невідома помилка";
        }
    }
}
