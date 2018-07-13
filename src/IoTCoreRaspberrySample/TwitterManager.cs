using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tweetinvi;
using Windows.Storage;

namespace IoTCoreRaspberrySample
{
    public class TwitterCredentials
    {
        public string CONSUMER_KEY { get; set; } = "";
        public string CONSUMER_SECRET { get; set; } = "";
        public string ACCESS_TOKEN { get; set; } = "";
        public string ACCESS_TOKEN_SECRET { get; set; } = "";
    }

    public class TwitterManager
    {
        private static StorageFolder _localFolder;
        private TwitterCredentials _credentials;

        public TwitterManager()
        {
            _localFolder = ApplicationData.Current.LocalFolder;
        }

        public async Task<bool> Setup()
        {
            _credentials = await LoadJson<TwitterCredentials>("credentials.txt") ?? new TwitterCredentials();

            //apenas para gerar o arquivo inicial
            SaveJson(_credentials, "credentials.txt");

            Auth.SetUserCredentials(_credentials.CONSUMER_KEY, _credentials.CONSUMER_SECRET, _credentials.ACCESS_TOKEN, _credentials.ACCESS_TOKEN_SECRET);
            var user = User.GetAuthenticatedUser();
            if (user == null || string.IsNullOrWhiteSpace(user.Name))
                return false;

            return true;
        }

        public async Task Publish(string text)
        {
            var result = Tweet.PublishTweet(text + " #W10IoTCore #TheDevConf");
        }

        public async Task<(bool on, string user, string image)> GetTweet()
        {
            var matchingTweets = await Task.Run(() => Search.SearchTweets("#TdcIot"));
            var tweet = matchingTweets.FirstOrDefault();
            if (tweet == null)
                return (false, null, null);

            var on = tweet.FullText.ToLowerInvariant().Contains("on");
            var name = $"{tweet.CreatedBy.Name}\n@{tweet.CreatedBy.ScreenName}";
            return (on, name, tweet.CreatedBy.ProfileImageUrl400x400);
        }

        public static async Task<T> LoadJson<T>(string file)
        {
            var f = await _localFolder.TryGetItemAsync(file);
            if (f == null)
                await _localFolder.CreateFileAsync(file);

            var readStream = await _localFolder.OpenStreamForReadAsync(file);
            var reader = new StreamReader(readStream);
            var str = reader.ReadToEnd();
            T result = JsonConvert.DeserializeObject<T>(str);
            return result;
        }

        public static async Task SaveJson<T>(T obj, string file) where T : class
        {
            var f = await _localFolder.TryGetItemAsync(file);
            if (f == null)
                await _localFolder.CreateFileAsync(file);

            var writeStream = await _localFolder.OpenStreamForWriteAsync(file, CreationCollisionOption.ReplaceExisting);
            var writer = new StreamWriter(writeStream);
            var str = JsonConvert.SerializeObject(obj, Formatting.Indented);
            await writer.WriteAsync(str);
            await writer.FlushAsync();
            writeStream.Dispose();
        }
    }
}
