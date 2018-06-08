using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using InstaSharper.API;
using InstaSharper.API.Builder;
using InstaSharper.Classes;
using InstaSharper.Classes.Models;
using InstaSharper.Logger;
using Path = System.IO.Path;

namespace InstaParser
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string Config { get; } = "config.txt";
        public string Settings { get; } = "settings.txt";
        public string UserFile { get; } = "user.txt";
        public string HorisontalHtml { get; } = "horisontal.html";
        public string BothStyle { get; } = "both_style.css";
        public string HorisontalStyle { get; } = "horisontal_style.css";
        public string VerticalHtml { get; } = "vertical.html";
        public string TextHtml { get; } = "text.html";
        public string TextStyle { get; } = "text_style.css";
        public string VerticalStyle { get; } = "vertical_style.css";
        public string SquareHtml { get; } = "square.html";
        public string SquareStyle { get; } = "square_style.css";
        public IInstaApi ApiInst;
        public UserSessionData User { set; get; } = new UserSessionData();
        public int MaxPages { get; set; } = int.MaxValue;
        public string HtmlPreview { get; } = "preview.html";
        public bool IsSettingsOpened { get; set; } = false;

        public string[] Month { get; } = new[]
        {
            "Января",
            "Февраля",
            "Марта",
            "Апреля",
            "Мая",
            "Июня",
            "Июля",
            "Августа",
            "Сентября",
            "Октября",
            "Ноября",
            "Декабря"
        };

        public MainWindow()
        {
            InitializeComponent();
            Loaded += Window_Loaded;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(Config))
            {
                HrefTextBox.Text = File.ReadAllText(Config);
            }

            if (File.Exists(Settings))
            {
                try
                {
                    var lines = File.ReadLines(Settings).ToList();
                    HeightTextBox.Text = lines[0];
                    WidthTextBox.Text = lines[1];
                    TopTextBox.Text = lines[2];
                    RightTextBox.Text = lines[3];
                    LeftTextBox.Text = lines[4];
                }
                catch (Exception exception)
                {
                    MessageBox.Show($"Ошибка чтения файла с настройками: {exception}", "Ошибка", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            if (File.Exists(UserFile))
            {
                var lines = File.ReadLines(UserFile).ToList();
                if (lines.Count == 2)
                {
                    AuthText.Content = "Авторизация, пожалуйста подождите...";
                    LoginTextBox.Visibility = Visibility.Hidden;
                    PasswordTextBox.Visibility = Visibility.Hidden;
                    AuthBtn.Visibility = Visibility.Hidden;
                    User.UserName = lines[0];
                    User.Password = lines[1];
                    ApiInst = InstaApiBuilder.CreateBuilder().SetUser(User).Build();
                    await ApiInst.LoginAsync();
                    if (ApiInst.IsUserAuthenticated)
                    {
                        AuthText.Content = "Авторизация";
                        string user = User.UserName.Length > 15
                            ? "" + User.UserName.Substring(0, 15) + "..."
                            : User.UserName;
                        ChangeAccBtn.Content = $"{user} сменить";
                        AuthGrid.Visibility = Visibility.Hidden;
                        LoginTextBox.Visibility = Visibility.Hidden;
                        PasswordTextBox.Visibility = Visibility.Hidden;
                        AuthBtn.Visibility = Visibility.Hidden;
                        PostsCountText.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        AuthText.Content = "Авторизация";
                        LoginTextBox.Visibility = Visibility.Visible;
                        PasswordTextBox.Visibility = Visibility.Visible;
                        AuthBtn.Visibility = Visibility.Visible;
                        MessageBox.Show("Авторизация не удалась. Введите логин и пароль заново", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

        }

        private async void HrefBtn_Click(object sender, RoutedEventArgs e)
        {
            if (HrefTextBox.Text.Length > 0)
            {
                List<int> maxMedia = new List<int>();
                try
                {
                    string[] maxMediasStr = PostsCountTextBox?.Text.Split(' ');
                    foreach (var maxMediaStr in maxMediasStr)
                    {
                        maxMedia.Add(int.Parse(maxMediaStr));
                    }
                }
                catch (Exception exception)
                {
                    MessageBox.Show("Введено некорректное количество публикаций", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                File.WriteAllText(Config, HrefTextBox.Text);
                HrefTextBox.Foreground = new SolidColorBrush(Color.FromRgb(154, 187, 218));
                HrefTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(154, 187, 218));
                HrefBtn.Background = new SolidColorBrush(Color.FromRgb(154, 187, 218));
                HrefBtn.BorderBrush = new SolidColorBrush(Color.FromRgb(154, 187, 218));
                HrefText.Foreground = new SolidColorBrush(Color.FromRgb(154, 187, 218));
                Win.Height = 120;
                PreviewBtn.Visibility = Visibility.Hidden;
                GeneratePdfBtn.Visibility = Visibility.Hidden;
                IsSettingsOpened = false;
                SettingsGrid.Visibility = Visibility.Hidden;
                var accs = HrefTextBox.Text.Split(new[] { '\r', '\n' }).Where(x => !string.IsNullOrEmpty(x)).ToArray();

                for (int i = 0; i < accs.Length; i++)
                {
                    try
                    {
                        HrefText.Content = "Получение данных..." + Environment.NewLine + accs[i];
                        Console.WriteLine(maxMedia[i]);
                        await ParseAccountAsync(accs[i], maxMedia[i] == 0 ? int.MaxValue : maxMedia[i]);
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show("Ошибка при парсинге, проверьте настройки", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                HrefText.Content = "Данные получены";
                HrefTextBox.Foreground = new SolidColorBrush(Color.FromRgb(242, 140, 164));
                HrefTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(242, 140, 164));
                HrefBtn.Background = new SolidColorBrush(Color.FromRgb(242, 140, 164));
                HrefBtn.BorderBrush = new SolidColorBrush(Color.FromRgb(242, 140, 164));
                HrefText.Foreground = new SolidColorBrush(Color.FromRgb(242, 140, 164));
                PreviewBtn.Visibility = Visibility.Visible;
                SettingsBtn.Visibility = Visibility.Visible;
                GeneratePdfBtn.Visibility = Visibility.Visible;
                Win.Height = 190;
            }
        }

        private async Task ParseAccountAsync(string username, int maxMedia)
        {
            var currentUserMedia = await ApiInst.GetUserMediaAsync(username, PaginationParameters.MaxPagesToLoad(MaxPages));
            if (currentUserMedia.Succeeded)
            {
                int num = 0;
                do
                {
                    num += 1;
                } while (Directory.Exists(username + num));

                string htmlAll = "<html><head><meta charset=\"UTF-8\"><style>";
                htmlAll += Environment.NewLine + File.ReadAllText(BothStyle);
                htmlAll += Environment.NewLine + File.ReadAllText(TextStyle);
                htmlAll += Environment.NewLine + File.ReadAllText(VerticalStyle);
                htmlAll += Environment.NewLine + File.ReadAllText(SquareStyle);
                htmlAll += Environment.NewLine + File.ReadAllText(HorisontalStyle);
                htmlAll += "</style></head><body> " + Environment.NewLine;
                var curDir = Directory.CreateDirectory(username + num);
                var imgDir = Directory.CreateDirectory(curDir.FullName + "/images/");
                var htmlDir = Directory.CreateDirectory(curDir.FullName + "/html/");
                int mediaNum = 0;

                foreach (var media in currentUserMedia.Value)
                {

                    if (mediaNum == maxMedia)
                    {
                        break;
                    }

                    if (mediaNum == 0)
                    {

                        using (WebClient client = new WebClient())
                        {
                            client.DownloadFile(media?.User?.ProfilePicture, imgDir.FullName + "profile.jpg");
                        }
                    }
                    String html = "";
                    var img = media.Images.Count > 0 ? media.Images[0] : media.Carousel[0].Images[0];
                    if (img != null)
                    {
                        if (img.Height > img.Width)
                        {
                            html += File.ReadAllText(VerticalHtml);
                        }
                        else if (img.Height == img.Width && media.Caption?.Text.Length > int.Parse(SquareTemplText.Text))
                        {

                            html += File.ReadAllText(SquareHtml);
                        }
                        else
                        {
                            html += File.ReadAllText(HorisontalHtml);
                        }
                    }

                    using (WebClient client = new WebClient())
                    {
                        client.DownloadFile(img?.URI, imgDir.FullName + mediaNum + ".jpg");
                    }

                    html = html?.Replace("REPLACENAME", media.User.UserName);
                    html = html?.Replace("REPLACEACCOUNTNAME", media.User.UserName + num);
                    html = html?.Replace("REPLACEGEO", media.Location?.ShortName ?? string.Empty);
                    html = html?.Replace("REPLACEPOSTIMG", imgDir.FullName + mediaNum + ".jpg");
                    html = html?.Replace("REPLACELIKE", media.LikesCount.ToString());
                    html = html?.Replace("REPLACEDATE", $"{media.TakenAt.Day} " +
                                                        $"{Month[media.TakenAt.Month - 1]} " +
                                                        $"{media.TakenAt.Year} года");
                    string caption = Regex.Replace(
                            RemoveSpecialChars(
                                    EmojiOne.EmojiOne.ToImage(
                                        media.Caption?.Text.Replace("⠀", String.Empty) ?? string.Empty))
                                .Replace("//", "https://").Replace("&#65039;", string.Empty), "[U].([0-9]*)",
                            string.Empty);
                    if (media.Caption?.Text.Length < int.Parse(NextPageText.Text))
                    {
                        html = html?.Replace("REPLACECAPTION", caption);
                    }
                    else if (string.IsNullOrEmpty(media.Caption?.Text))
                    {
                        html = html?.Replace("REPLACECAPTION", string.Empty);
                    }
                    else
                    {
                        html = html?.Replace("REPLACECAPTION", string.Empty);
                        html += File.ReadAllText(TextHtml);
                        html = html?.Replace("REPLACECAPTION", caption);
                    }
                    File.WriteAllText(htmlDir.FullName + username + mediaNum + ".html", html);
                    htmlAll += Environment.NewLine + html;
                    mediaNum += 1;
                }
                htmlAll += Environment.NewLine + "</body></html>";
                File.WriteAllText($"{username}.html", htmlAll);
            }
            else
            {
                Console.WriteLine("error");
            }
        }

        private void PreviewBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var accs = HrefTextBox.Text.Split(new[] { '\r', '\n' }).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                foreach (var userAcc in accs)
                {
                    Process.Start("chrome.exe", $"{Directory.GetCurrentDirectory()}/{userAcc}.html");
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Ошибка: {exception}");
            }
        }

        string RemoveSpecialChars(string input)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in input)
            {
                if ((c >= '0' && c <= '9') || (c >= 'а' && c <= 'я') || (c >= 'А' && c <= 'Я') || (c >= 'A' && c <= 'Z')
                    || (c >= 'a' && c <= 'z') || c == '.' || c == '-' || c == '—' || c == '—' || c == '$' || c == '€' || c == '_'
                    || c == ' ' || c == '/' || c == '\\' || c == '|' || c == '\"' || c == '\'' || c == '?' || c == '!' || c == '@'
                    || c == '%' || c == '^' || c == ':' || c == ';' || c == '&' || c == '*' || c == '(' || c == ')' || c == '+'
                    || c == '=' || c == '~' || c == ',' || c == '<' || c == '>' || c == '{' || c == '}' || c == '[' || c == ']'
                    || c == '#' || c == '№' || c == 'ё' || c == 'Ё')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private void GeneratePdfBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(Directory.GetCurrentDirectory() + "/generated"))
                    Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/generated");
                var accs = HrefTextBox.Text.Split(new[] { '\r', '\n' }).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                foreach (var userAcc in accs)
                {
                    var genPdf = Process.Start(
                        $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/wkhtmltopdf/bin/wkhtmltopdf.exe",
                        $"--encoding UTF-8 --page-height {HeightTextBox.Text}cm --page-width {WidthTextBox.Text}cm -T {TopTextBox.Text}cm -R {RightTextBox.Text}cm -B {BottomTextBox.Text}cm -L {LeftTextBox.Text}cm {userAcc}.html --footer-right [page] {Directory.GetCurrentDirectory()}/generated/{userAcc}.pdf");
                    genPdf?.WaitForExit();
                }
                MessageBox.Show("PDF файлы сгенирированы и находятся в папке generated");
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Ошибка: {exception}");
            }
        }

        private void SettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!IsSettingsOpened)
            {
                IsSettingsOpened = true;
                SettingsGrid.Visibility = Visibility.Visible;
                Win.Height = 480;
                SettingsBtn.Foreground = new SolidColorBrush(Color.FromRgb(242, 140, 164));
                SettingsBtn.BorderBrush = new SolidColorBrush(Color.FromRgb(242, 140, 164));
            }
            else
            {
                IsSettingsOpened = false;
                SettingsGrid.Visibility = Visibility.Hidden;
                Win.Height = 120;
                SettingsBtn.Foreground = new SolidColorBrush(Color.FromRgb(154, 187, 218));
                SettingsBtn.BorderBrush = new SolidColorBrush(Color.FromRgb(154, 187, 218));
            }
        }

        private async void AuthBtn_Click(object sender, RoutedEventArgs e)
        {
            User.UserName = LoginTextBox.Text;
            User.Password = PasswordTextBox.Text;
            ApiInst = InstaApiBuilder.CreateBuilder().SetUser(User).Build();
            await ApiInst.LoginAsync();
            if (ApiInst.IsUserAuthenticated)
            {
                File.WriteAllText(UserFile, User.UserName + Environment.NewLine + User.Password);
                ChangeAccBtn.Content = $"{User.UserName} сменить";
                AuthGrid.Visibility = Visibility.Hidden;
            }
            else
            {
                MessageBox.Show("Ошибка авторизации. Проверьте правильность ввода логина и пароля", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChangeAccBtn_Click(object sender, RoutedEventArgs e)
        {
            LoginTextBox.Visibility = Visibility.Visible;
            PasswordTextBox.Visibility = Visibility.Visible;
            AuthBtn.Visibility = Visibility.Visible;
            AuthGrid.Visibility = Visibility.Visible;
        }

        private void ClearCacheBtn_Click(object sender, RoutedEventArgs e)
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            foreach (var file in dir.GetFiles())
            {
                if (file.FullName.Contains("html") && (!file.FullName.Contains("horisontal") || !file.FullName.Contains("vertical") || !file.FullName.Contains("square") || !file.FullName.Contains("text")))
                {
                    file.Delete();
                }
            }
        }
    }
}
