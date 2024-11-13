using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using WebSiteElectronicMind.Rendering.Methods;
using WebSiteElectronicMind.Rendering.Methods.Input;
using WebSiteElectronicMind.Rendering.Methods.OL;
using WebSiteElectronicMind.Rendering.Methods.Shina;
using ColorImageSharp = SixLabors.ImageSharp.Color;

using Color = SixLabors.ImageSharp.Color;
using WebSiteElectronicMind.Core.Models.RenderingToPDF;
using WebSiteElectronicMind.ML.Repositories;

namespace WebSiteElectronicMind.Rendering.Repositories
{
    public class PdfGeneratorMoreSevenRepositories : IPdfGeneratorMoreSevenRepositories
    {
        private const int A4Width = 2480;
        private const int A4Height = 3508;

        private readonly Pattern _pattern;
        private readonly Input _automatP1;
        private readonly Shina _shina;
        private readonly OL _oL;
        private readonly IGetCharacteristicRepositories _getCharacteristicRepositories;
        private readonly NoteMoreSeven _note;

        public PdfGeneratorMoreSevenRepositories(Input automatP1, Shina shina, OL ol, Pattern pattern, NoteMoreSeven note, IGetCharacteristicRepositories getCharacteristicRepositories)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            _pattern = pattern;
            _automatP1 = automatP1;
            _shina = shina;
            _oL = ol;
            _getCharacteristicRepositories = getCharacteristicRepositories;
            _note = note;
        }

        public async Task GeneratePdfMoreSevenAsync(string outputPdfPath, int numberOfAutomats, List<TablePDF> tablePDF, Table1C table1C)
        {
            // Предварительно получаем все полюса для таблицы асинхронно
            var polusList = new List<int>();
            foreach (var item in tablePDF)
            {
                int polus = await _getCharacteristicRepositories.GetPolus(item.Name);
                polusList.Add(polus);
            }

            // Настройки шрифта
            var fontCollection = new FontCollection();
            var fontFamily = fontCollection.Add("Files/Font/arialmt.ttf");
            var font = fontFamily.CreateFont(24, FontStyle.Regular);

            // Создание документа PDF с QuestPDF
            var document = Document.Create(container =>
            {
                const int itemsPerPage = 7;
                int totalPages = (int)Math.Ceiling((double)numberOfAutomats / itemsPerPage);

                for (int pageIndex = 0; pageIndex < totalPages; pageIndex++)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(0);

                        // Генерация содержимого страницы
                        using (Image<Rgba32> canvas = new Image<Rgba32>(A4Width, A4Height))
                        {
                            // Заливаем фон белым цветом
                            canvas.Mutate(x => x.Fill(ColorImageSharp.White));

                            // Координаты и шаги для размещения элементов
                            int startX = 500;
                            int startY = 1100;
                            int stepX = 350;

                            int startIndex = pageIndex * itemsPerPage;
                            int endIndex = Math.Min(startIndex + itemsPerPage, numberOfAutomats);

                            for (int i = startIndex; i < endIndex; i++)
                            {
                                var data = tablePDF[i];
                                int localIndex = i - startIndex;
                                int posX;

                                if (pageIndex == 0) // Логика для первой страницы
                                {
                                    // Первый элемент на месте, остальные сдвигаются на один слот вправо
                                    posX = startX + ((localIndex == 0) ? 0 : (localIndex - 1) * stepX);
                                }
                                else // Логика для второй и последующих страниц
                                {
                                    // Элементы идут последовательно
                                    posX = startX + (localIndex * stepX);
                                }

                                // Используем предварительно собранные полюса
                                int polus = polusList[i];

                                // Отрисовка в зависимости от уровня
                                switch (data.Level)
                                {
                                    case 1:
                                        RenderInput(canvas, data.Type, polus);
                                        break;
                                    case 2:
                                        RenderAutomat(canvas, data.Type, polus, posX, startY);
                                        break;
                                    default:
                                        throw new ArgumentException("Неизвестный уровень оборудования");
                                }

                                // Разбивка строки схемы для отображения построчно
                                var schemeParts = data.NameOfScheme.Split(' ');
                                int startYSchem = 1430;
                                int lineHeight = 30;
                                int textOffsetX = -150;

                                // Отрисовка текстовых элементов
                                if (localIndex == 0 && data.Level == 1)
                                {
                                    // Для оборудования уровня 1 (вводное устройство)
                                    canvas.Mutate(x =>
                                    {
                                        x.DrawText(data.NumberingLetter, font, Color.Black, new PointF(230, 400));
                                        for (int j = 0; j < schemeParts.Length; j++)
                                        {
                                            x.DrawText(schemeParts[j], font, Color.Black, new PointF(230, 430 + j * lineHeight));
                                        }
                                    });
                                }
                                else
                                {
                                    // Для остальных уровней
                                    canvas.Mutate(x =>
                                    {
                                        x.DrawText(data.NumberingLetter, font, Color.Black, new PointF(posX + textOffsetX, 1400));
                                        for (int j = 0; j < schemeParts.Length; j++)
                                        {
                                            x.DrawText(schemeParts[j], font, Color.Black, new PointF(posX + textOffsetX, startYSchem + j * lineHeight));
                                        }
                                    });
                                }
                            }


                            // Отрисовка шины в зависимости от номинального напряжения
                            switch (table1C.Electrical.NominalVoltage)
                            {
                                case 230:
                                    _shina.CreateShina230B(canvas);
                                    break;
                                case 400:
                                    _shina.CreateShina400B(canvas);
                                    break;
                                default:
                                    throw new ArgumentException("Некорректное номинальное напряжение для шины");
                            }

                            // Добавление декоративных элементов и комментариев
                            _pattern.Decoration(canvas);
                            _note.Comment(canvas, table1C, pageIndex + 1, totalPages);

                            // Сохранение изображения и добавление в PDF
                            using (var imageStream = new MemoryStream())
                            {
                                canvas.SaveAsPng(imageStream);
                                imageStream.Seek(0, SeekOrigin.Begin);
                                page.Content().Image(imageStream.ToArray());
                            }
                        }
                    });
                }
            });

            // Генерация PDF и сохранение на диск
            document.GeneratePdf(outputPdfPath);
        }



        private void RenderInput(Image<Rgba32> canvas, string type, int polus)
        {
            switch (type)
            {
                case "Автомат":
                    switch (polus)
                    {
                        case 1:
                            _automatP1.CreateAutomatP1(canvas, new Point(360, 200));
                            break;
                        case 3:
                            _automatP1.CreateAutomatP3(canvas, new Point(360, 200));
                            break;
                        default:
                            throw new ArgumentException("Некорректное количество полюсов для входного автомата");
                    }
                    break;

                case "Диф автомат":
                    switch (polus)
                    {
                        case 1:
                            _automatP1.CreateDifAutomatP1(canvas, new Point(360, 200));
                            break;
                        case 3:
                            _automatP1.CreateDifAutomatP3(canvas, new Point(360, 200));
                            break;
                        default:
                            throw new ArgumentException("Некорректное количество полюсов для входного диф автомата");
                    }
                    break;

                case "УЗО":
                    switch (polus)
                    {
                        case 1:
                            _automatP1.CreateYZoP1(canvas, new Point(360, 200));
                            break;
                        case 3:
                            _automatP1.CreateYZoP3(canvas, new Point(360, 200));
                            break;
                        default:
                            throw new ArgumentException("Некорректное количество полюсов для УЗО");
                    }
                    break;
            }
        }

        private void RenderAutomat(Image<Rgba32> canvas, string type, int polus, int posX, int posY)
        {
            switch (type)
            {
                case "Автомат":
                    switch (polus)
                    {
                        case 1:
                            _oL.CreateAutomatP1(canvas, new Point(posX, posY));
                            break;
                        case 3:
                            _oL.CreateAutomatP3(canvas, new Point(posX, posY));
                            break;
                    }
                    break;

                case "Диф автомат":
                    switch (polus)
                    {
                        case 1:
                            _oL.CreateDifAutomatP1(canvas, new Point(posX, posY));
                            break;
                        case 3:
                            _oL.CreateDifAutomatP3(canvas, new Point(posX, posY));
                            break;
                    }
                    break;

                case "УЗО":
                    switch (polus)
                    {
                        case 1:
                            _oL.CreateYZoP1(canvas, new Point(posX, posY));
                            break;
                        case 3:
                            _oL.CreateYZoP3(canvas, new Point(posX, posY));
                            break;
                    }
                    break;
            }
        }
    }
}
