using ElectronicStudyCabinet.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStudyCabinet.Data;

public static class DemoDataSeeder
{
    // Описание данных (простые записи, из них ниже строятся настоящие объекты)
    private record LectureSeed(string Title, string Description, int PassingPercentage = 70, int MaxAttempts = 3);
    private record TopicSeed(string Name, string Description, LectureSeed[] Lectures);
    private record DisciplineSeed(string Name, string Description, bool IsPublished, TopicSeed[] Topics);

    private static readonly DisciplineSeed[] Data =
    {
        new("Внедрение и поддержка компьютерных систем",
            "Установка, настройка, внедрение и сопровождение компьютерных систем в организации.",
            true,
            new TopicSeed[]
            {
                new("Основы компьютерных систем",
                    "Состав и принципы работы компьютерной системы.",
                    new LectureSeed[]
                    {
                        new("Состав и архитектура компьютерной системы",
                            "Аппаратные и программные компоненты и их взаимодействие."),
                        new("Операционные системы",
                            "Назначение, виды и основные функции операционных систем."),
                        new("Установка и настройка операционной системы",
                            "Порядок установки, первоначальная настройка и обновление.", 70, 3)
                    }),
                new("Внедрение программного обеспечения",
                    "Подготовка и проведение внедрения программных продуктов.",
                    new LectureSeed[]
                    {
                        new("Планирование внедрения",
                            "Анализ требований, этапы и сроки внедрения."),
                        new("Установка и обновление программного обеспечения",
                            "Развёртывание программ, управление версиями и обновлениями."),
                        new("Обучение пользователей",
                            "Подготовка инструкций и обучение сотрудников.", 60, 5)
                    }),
                new("Сопровождение и диагностика",
                    "Поддержка работоспособности системы после внедрения.",
                    new LectureSeed[]
                    {
                        new("Мониторинг и диагностика неисправностей",
                            "Инструменты наблюдения за системой и поиск причин сбоев."),
                        new("Резервное копирование и восстановление",
                            "Планирование копирования данных и восстановление после сбоя.", 80, 2)
                    })
            }),

        new("Основы программирования на C#",
            "Базовый курс по языку C# и платформе .NET.",
            true,
            new TopicSeed[]
            {
                new("Введение в язык C#",
                    "Знакомство со средой разработки и основами языка.",
                    new LectureSeed[]
                    {
                        new("Среда разработки и первая программа",
                            "Установка Visual Studio, создание и запуск консольного приложения."),
                        new("Типы данных и переменные",
                            "Числа, строки, логический тип, объявление и преобразование."),
                        new("Операторы и выражения",
                            "Арифметические, логические операторы и приоритет операций.")
                    }),
                new("Управляющие конструкции",
                    "Ветвления, циклы и работа с наборами данных.",
                    new LectureSeed[]
                    {
                        new("Условные операторы",
                            "Конструкции if, else и switch."),
                        new("Циклы",
                            "Циклы for, while и foreach, управление выполнением цикла."),
                        new("Массивы и коллекции",
                            "Массивы, списки и словари, перебор элементов.", 80, 2)
                    })
            }),

        new("Базы данных и PostgreSQL",
            "Проектирование баз данных и работа с PostgreSQL.",
            true,
            new TopicSeed[]
            {
                new("Реляционная модель",
                    "Основные понятия и проектирование баз данных.",
                    new LectureSeed[]
                    {
                        new("Основные понятия баз данных",
                            "Таблицы, строки, ключи и связи между таблицами."),
                        new("Проектирование и нормализация",
                            "ER-диаграммы и нормальные формы.")
                    }),
                new("Язык SQL",
                    "Запросы на выборку и изменение данных.",
                    new LectureSeed[]
                    {
                        new("Выборка данных: SELECT и WHERE",
                            "Чтение данных, фильтрация, сортировка и группировка."),
                        new("Соединение таблиц (JOIN)",
                            "Внутренние и внешние соединения таблиц.", 75, 3),
                        new("Изменение данных: INSERT, UPDATE, DELETE",
                            "Добавление, изменение и удаление записей, транзакции.")
                    }),
                new("Администрирование PostgreSQL",
                    "Резервное копирование и управление доступом.",
                    new LectureSeed[]
                    {
                        new("Резервное копирование и восстановление",
                            "Утилиты pg_dump и pg_restore."),
                        new("Роли и права доступа",
                            "Создание ролей и назначение прав на объекты базы.")
                    })
            }),

        new("Компьютерные сети",
            "Принципы построения и работы компьютерных сетей.",
            true,
            new TopicSeed[]
            {
                new("Основы сетей",
                    "Базовые понятия, модели и протоколы.",
                    new LectureSeed[]
                    {
                        new("Модели OSI и TCP/IP",
                            "Уровни сетевых моделей и их назначение."),
                        new("IP-адресация и маски подсетей",
                            "Адреса IPv4, маски, деление сети на подсети.", 70, 4),
                        new("Протоколы DNS и DHCP",
                            "Автоматическая выдача адресов и преобразование имён.")
                    }),
                new("Сетевое оборудование",
                    "Устройства, из которых строятся сети.",
                    new LectureSeed[]
                    {
                        new("Коммутаторы и маршрутизаторы",
                            "Назначение и принципы работы сетевых устройств."),
                        new("Беспроводные сети Wi-Fi",
                            "Стандарты, настройка и защита беспроводных сетей.")
                    })
            }),

        // Черновик: студентам не виден, пока преподаватель не опубликует
        new("Информационная безопасность",
            "Защита информации и безопасная работа с компьютерными системами.",
            false,
            new TopicSeed[]
            {
                new("Основы защиты информации",
                    "Угрозы и базовые меры защиты.",
                    new LectureSeed[]
                    {
                        new("Угрозы информационной безопасности",
                            "Виды угроз, источники и последствия."),
                        new("Парольная политика и аутентификация",
                            "Требования к паролям, двухфакторная проверка."),
                        new("Защита от вредоносного ПО",
                            "Вирусы, антивирусные средства и правила безопасной работы.")
                    }),
                new("Защита данных",
                    "Шифрование и контроль доступа.",
                    new LectureSeed[]
                    {
                        new("Шифрование данных",
                            "Симметричное и асимметричное шифрование, хэш-функции."),
                        new("Контроль доступа и аудит",
                            "Разграничение прав и ведение журналов событий.")
                    })
            })
    };

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        // Отключается настройкой Seed:DemoData = false в appsettings.json
        if (!configuration.GetValue<bool>("Seed:DemoData", true))
            return;

        // Если в базе уже есть дисциплины, ничего не добавляем
        if (await db.Disciplines.AnyAsync())
            return;

        foreach (var disciplineSeed in Data)
        {
            var discipline = new Discipline
            {
                Name = disciplineSeed.Name,
                Description = disciplineSeed.Description,
                IsPublished = disciplineSeed.IsPublished,
                CreatedAt = DateTime.UtcNow
            };

            var topicOrder = 0;
            foreach (var topicSeed in disciplineSeed.Topics)
            {
                topicOrder++;
                var topic = new Topic
                {
                    Name = topicSeed.Name,
                    Description = topicSeed.Description,
                    OrderIndex = topicOrder
                };

                var lectureOrder = 0;
                foreach (var lectureSeed in topicSeed.Lectures)
                {
                    lectureOrder++;
                    topic.Lectures.Add(new Lecture
                    {
                        Title = lectureSeed.Title,
                        Description = lectureSeed.Description,
                        OrderIndex = lectureOrder,
                        IsPublished = true,
                        PassingPercentage = lectureSeed.PassingPercentage,
                        MaxAttempts = lectureSeed.MaxAttempts,
                        RequireControlQuestions = false
                    });
                }

                discipline.Topics.Add(topic);
            }

            db.Disciplines.Add(discipline);
        }

        // Всё сохраняется одной транзакцией: либо все данные, либо ничего
        await db.SaveChangesAsync();

        logger.LogInformation("Добавлены демонстрационные данные: дисциплин — {Count}.", Data.Length);
    }
}