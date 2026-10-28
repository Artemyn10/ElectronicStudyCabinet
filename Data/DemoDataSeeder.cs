using ElectronicStudyCabinet.Models;
using Microsoft.EntityFrameworkCore;
using ElectronicStudyCabinet.Services;

namespace ElectronicStudyCabinet.Data;

public static class DemoDataSeeder
{
    // Описание данных (простые записи, из них ниже строятся настоящие объекты)
    private record LectureSeed(string Title, string Description, int PassingPercentage = 70, int MaxAttempts = 3);
    private record TopicSeed(string Name, string Description, LectureSeed[] Lectures);
    private record DisciplineSeed(string Name, string Description, bool IsPublished, TopicSeed[] Topics);
    // ===== Дополнительные демо-данные: файлы и контрольные вопросы =====

    private record MaterialSeed(string DiskFileName, string DisplayName);

    private record QuestionSeed(
        string Text,
        QuestionType Type,
        string? CorrectAnswer = null,
        bool IsRequired = true,
        bool ManualReview = false,
        string[]? Options = null,
        int CorrectOption = 0);

    private record LectureExtras(
        string LectureTitle,
        bool RequireQuestions,
        MaterialSeed? Material,
        QuestionSeed[] Questions);

    private static readonly LectureExtras[] Extras =
    {
        new("Состав и архитектура компьютерной системы",
            true,
            new MaterialSeed("computer-architecture.docx", "Состав и архитектура компьютерной системы.docx"),
            new QuestionSeed[]
            {
                new("Как называется основное устройство компьютера, которое выполняет команды программ?",
                    QuestionType.ShortText,
                    CorrectAnswer: "процессор"),
                new("Какое из перечисленных устройств предназначено для долговременного хранения данных?",
                    QuestionType.SingleChoice,
                    Options: new[]
                    {
                        "Оперативная память (RAM)",
                        "Кэш-память процессора",
                        "Твердотельный накопитель (SSD)",
                        "Видеокарта"
                    },
                    CorrectOption: 2)
            }),

        new("Операционные системы",
            true,
            new MaterialSeed("os-lecture.pdf", "Операционные системы.pdf"),
            new QuestionSeed[]
            {
                new("Объясните своими словами, чем ядро операционной системы отличается от пользовательской оболочки.",
                    QuestionType.ShortText,
                    CorrectAnswer: "Ядро управляет оборудованием и ресурсами, оболочка предоставляет пользователю интерфейс.",
                    ManualReview: true),
                new("Какая из перечисленных программ является операционной системой?",
                    QuestionType.SingleChoice,
                    Options: new[] { "Microsoft Word", "Linux", "PostgreSQL", "Google Chrome" },
                    CorrectOption: 1)
            }),

        new("Типы данных и переменные",
            false,
            null,
            new QuestionSeed[]
            {
                new("Какой тип данных C# предназначен для хранения целых чисел?",
                    QuestionType.SingleChoice,
                    Options: new[] { "string", "int", "bool", "double" },
                    CorrectOption: 1),
                new("Какое ключевое слово позволяет объявить переменную с автоматическим выводом типа?",
                    QuestionType.ShortText,
                    CorrectAnswer: "var",
                    IsRequired: false)
            })
    };

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
    // ===== Демо-тесты =====

    private record TestQuestionSeed(string Text, string[] Options, int CorrectOption);
    private record TestSeed(string LectureTitle, TestQuestionSeed[] Questions);

    private static readonly TestSeed[] DemoTests =
    {
        new("Состав и архитектура компьютерной системы", new TestQuestionSeed[]
        {
            new("Что относится к аппаратному обеспечению компьютера?",
                new[] { "Операционная система", "Процессор", "Текстовый редактор", "Драйвер" }, 1),
            new("Какое устройство временно хранит данные и программы во время работы?",
                new[] { "Накопитель SSD", "Оперативная память (RAM)", "Видеокарта", "Монитор" }, 1),
            new("Какой принцип относится к архитектуре фон Неймана?",
                new[]
                {
                    "Программа хранится в памяти вместе с данными",
                    "Данные хранятся только на внешнем накопителе",
                    "Команды выполняются одновременно в произвольном порядке",
                    "Информация представляется только в десятичном виде"
                }, 0),
            new("К какому виду программного обеспечения относятся драйверы?",
                new[] { "Прикладному", "Системному", "Инструментальному", "Игровому" }, 1),
            new("Какое устройство соединяет все компоненты компьютера?",
                new[] { "Процессор", "Материнская плата", "Видеокарта", "Монитор" }, 1)
        }),

        new("Операционные системы", new TestQuestionSeed[]
        {
            new("Какая функция относится к операционной системе?",
                new[]
                {
                    "Управление памятью и процессами",
                    "Создание презентаций",
                    "Редактирование фотографий",
                    "Просмотр веб-страниц"
                }, 0),
            new("Что такое ядро операционной системы?",
                new[]
                {
                    "Центральная часть, работающая с оборудованием и ресурсами",
                    "Графический рабочий стол",
                    "Набор офисных программ",
                    "Внешний накопитель"
                }, 0),
            new("Какая из перечисленных систем относится к семейству Linux?",
                new[] { "Ubuntu", "Windows 11", "iOS", "macOS" }, 0),
            new("Какая операционная система предназначена для смартфонов?",
                new[] { "Windows Server", "Android", "Debian", "Astra Linux" }, 1),
            new("Для чего нужен драйвер?",
                new[]
                {
                    "Чтобы операционная система могла работать с устройством",
                    "Чтобы защищать компьютер от вирусов",
                    "Чтобы хранить файлы пользователя",
                    "Чтобы выводить изображение на монитор"
                }, 0)
        }),

        new("Типы данных и переменные", new TestQuestionSeed[]
        {
            new("Какой тип данных C# используется для хранения текста?",
                new[] { "int", "string", "bool", "double" }, 1),
            new("Какой тип данных хранит значения true и false?",
                new[] { "bool", "string", "int", "char" }, 0),
            new("Какой тип подходит для хранения дробных чисел?",
                new[] { "int", "string", "double", "char" }, 2)
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
    // Добавляет демо-файлы и контрольные вопросы к уже созданным лекциям
    public static async Task SeedExtrasAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var storage = services.GetRequiredService<IFileStorageService>();
        var env = services.GetRequiredService<IWebHostEnvironment>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        if (!configuration.GetValue<bool>("Seed:DemoData", true))
            return;

        // Добавляем только если вопросов и файлов в базе ещё нет совсем
        var addQuestions = !await db.ControlQuestions.AnyAsync();
        var addFiles = !await db.Lectures.AnyAsync(l => l.MaterialPath != null);

        if (!addQuestions && !addFiles)
            return;

        var demoFilesDir = Path.Combine(env.ContentRootPath, "DemoFiles");
        var copiedFiles = new List<string>();

        try
        {
            foreach (var extra in Extras)
            {
                var lecture = await db.Lectures
                    .Include(l => l.ControlQuestions)
                    .FirstOrDefaultAsync(l => l.Title == extra.LectureTitle);

                if (lecture == null)
                    continue;

                if (addQuestions)
                {
                    lecture.RequireControlQuestions = extra.RequireQuestions;

                    var order = 0;
                    foreach (var seed in extra.Questions)
                    {
                        order++;
                        var isShortText = seed.Type == QuestionType.ShortText;

                        var question = new ControlQuestion
                        {
                            Text = seed.Text,
                            QuestionType = seed.Type,
                            CorrectAnswer = isShortText ? seed.CorrectAnswer : null,
                            IsRequired = seed.IsRequired,
                            RequiresManualReview = isShortText && seed.ManualReview,
                            OrderIndex = order
                        };

                        if (seed.Options != null)
                        {
                            for (var i = 0; i < seed.Options.Length; i++)
                            {
                                question.Options.Add(new ControlQuestionOption
                                {
                                    Text = seed.Options[i],
                                    IsCorrect = i == seed.CorrectOption,
                                    OrderIndex = i + 1
                                });
                            }
                        }

                        lecture.ControlQuestions.Add(question);
                    }
                }

                if (addFiles && extra.Material != null)
                {
                    var sourcePath = Path.Combine(demoFilesDir, extra.Material.DiskFileName);

                    if (File.Exists(sourcePath))
                    {
                        var storedName = await storage.CopyFromAsync(sourcePath);
                        copiedFiles.Add(storedName);

                        lecture.MaterialPath = storedName;
                        lecture.MaterialFileName = extra.Material.DisplayName;
                    }
                    else
                    {
                        logger.LogWarning("Демо-файл не найден: {Path}", sourcePath);
                    }
                }
            }

            await db.SaveChangesAsync();
        }
        catch
        {
            // Если сохранить в базу не удалось, убираем уже скопированные файлы
            foreach (var file in copiedFiles)
                storage.Delete(file);

            throw;
        }

        logger.LogInformation(
            "Добавлены демо-вопросы: {Questions}, демо-файлы: {Files}.",
            addQuestions, copiedFiles.Count);
    }
    // Добавляет опубликованные демо-тесты (если в базе ещё нет ни одного теста)
    public static async Task SeedTestsAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        if (!configuration.GetValue<bool>("Seed:DemoData", true))
            return;

        if (await db.Tests.AnyAsync())
            return;

        var added = 0;

        foreach (var seed in DemoTests)
        {
            var lecture = await db.Lectures.FirstOrDefaultAsync(l => l.Title == seed.LectureTitle);
            if (lecture == null)
                continue;

            var test = new Test { LectureId = lecture.Id, IsPublished = true };

            var questionOrder = 0;
            foreach (var questionSeed in seed.Questions)
            {
                questionOrder++;
                var question = new TestQuestion
                {
                    Text = questionSeed.Text,
                    OrderIndex = questionOrder
                };

                for (var i = 0; i < questionSeed.Options.Length; i++)
                {
                    question.Options.Add(new AnswerOption
                    {
                        Text = questionSeed.Options[i],
                        IsCorrect = i == questionSeed.CorrectOption,
                        OrderIndex = i + 1
                    });
                }

                test.Questions.Add(question);
            }

            db.Tests.Add(test);
            added++;
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Добавлены демонстрационные тесты: {Count}.", added);
    }
}