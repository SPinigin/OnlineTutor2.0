namespace OnlineTutor2.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public int? StatusCode { get; set; }
        public string? Message { get; set; }
        public string? Path { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        
        public string GetErrorMessage()
        {
            return StatusCode switch
            {
                400 => "Неверный запрос. Пожалуйста, проверьте введенные данные.",
                401 => "Требуется авторизация для доступа к этому ресурсу.",
                403 => "У вас нет прав для доступа к этому ресурсу.",
                404 => "Запрашиваемая страница не найдена.",
                500 => "Произошла внутренняя ошибка сервера. Мы уже работаем над ее устранением.",
                _ => "Произошла ошибка при обработке вашего запроса."
            };
        }
    }
}
