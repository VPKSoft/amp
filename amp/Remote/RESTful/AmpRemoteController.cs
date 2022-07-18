#region License
/*
MIT License

Copyright(c) 2021 Petteri Kautonen

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace amp.Remote.RESTful;

/// <summary>
/// A remote REST API controller for the amp# software.
/// Implements the <see cref="Controller" />
/// </summary>
/// <seealso cref="Controller" />
public class AmpRemoteController
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AmpRemoteController"/> class.
    /// </summary>
    /// <param name="baseUrl">The base URL.</param>
    public static void CreateInstance(string baseUrl)
    {
        InstanceContext?.Dispose();

        WebHost.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddMvc(options => options.EnableEndpointRouting = false))
            .Configure(app => app.UseMvc())
            .UseUrls(baseUrl)
            .Build()
            .RunAsync();
    }

    /// <summary>
    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources of the <see cref="InstanceContext"/> instance.
    /// </summary>
    /// </summary>
    public static void Dispose()
    {
        InstanceContext?.Dispose();
    }

    /// <summary>
    /// Gets or sets the instance context of this <see cref="AmpRemoteController"/> class.
    /// </summary>
    /// <value>The instance context of this <see cref="AmpRemoteController"/> class.</value>
    public static IWebHost InstanceContext { get; set; }
}