using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestApplication
{
    public class MyAsyncClass
    {
        public void Run()
        {
            NoRetvalAsync("John", 42).Wait();
            string x = StringRetvalAsync("John").Result;
            string x2 = GenericRetvalAsync<string>("John").Result;
            int x3 = GenericRetvalAsync<int>("John").Result;

            GenericClassWithAsync<int> xC = new GenericClassWithAsync<int>();
            int x4 = xC.DoAsync(42).Result;

            //exception
            try
            {
                NoRetvalAsync(null, 42).Wait();

            }
            catch (Exception)
            {}
        }

        public async Task<string> StringRetvalAsync(string input)
        {
            await NoRetvalAsync(input, 42);
            int x = await IntRetvalAsync(input);
            return $"{input}Ret";
        }

        public async Task NoRetvalAsync(string inp1, int inp2)
        {
            int inner = await IntRetvalAsync(inp1);
        }

        public async Task<int> IntRetvalAsync(string inp1)
        {
            await Task.Run(() => Thread.Sleep(10));
            if (inp1 == null) throw new ApplicationException("Failure");
            return 42;
        }

        public async Task<T> GenericRetvalAsync<T>(string input)
        {
            await NoRetvalAsync(input, 42);
            return default(T);
        }

        public class GenericClassWithAsync<T>
        {
            public async Task<T> DoAsync(T inp)
            {
                await DoNothingAsync();
                return inp;
            }

            public async Task DoNothingAsync()
            {
                await Task.Run(() => Thread.Sleep(10));
            }
        }
    }
}
