using System.Globalization;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;

public class JsonStringLocalizer : IStringLocalizer
{
  private readonly JsonSerializer _serializer = new();

  public LocalizedString this[string name]
  {
    get
    {
      string value = GetString(name);
      return new LocalizedString(name, value ?? name, value == null);
    }
  }

  public LocalizedString this[string name, params object[] arguments]
  {
    get
    {
      var actualValue = this[name];
      return !actualValue.ResourceNotFound
        ? new LocalizedString(name, string.Format(actualValue.Value, arguments), false)
        : actualValue;
    }
  }

  public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
  {
    string filePath = $"Resources/{Thread.CurrentThread.CurrentCulture.Name}.json";
    using (var str = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
    using (var sReader = new StreamReader(str))
    using (var reader = new JsonTextReader(sReader))
    {
      while (reader.Read())
      {
        if (reader.TokenType != JsonToken.PropertyName)
          continue;
        string key = (string)reader.Value;
        reader.Read();
        string value = _serializer.Deserialize<string>(reader);
        yield return new LocalizedString(key, value, false);
      }
    }
  }

  private string GetString(string key)
  {
    string relativeFilePath = $"Resources/{Thread.CurrentThread.CurrentCulture.Name}.json";
    string fullFilePath = Path.GetFullPath(relativeFilePath);
    if (!File.Exists(fullFilePath))
    {
      return default(string);
    }

    string result = GetValueFromJson(key, Path.GetFullPath(relativeFilePath));
    return result;
  }

  private string GetValueFromJson(string propertyName, string filePath)
  {
    if (propertyName == null) return default;
    if (filePath == null) return default;
    using var str = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
    using var sReader = new StreamReader(str);
    using var reader = new JsonTextReader(sReader);
    while (reader.Read())
    {
      if (reader.TokenType == JsonToken.PropertyName && (string)reader.Value == propertyName)
      {
        reader.Read();
        return _serializer.Deserialize<string>(reader);
      }
    }

    return default;
  }
}

public class JsonStringLocalizerFactory : IStringLocalizerFactory
{
  public IStringLocalizer Create(Type resourceSource) =>
    new JsonStringLocalizer();

  public IStringLocalizer Create(string baseName, string location) =>
    new JsonStringLocalizer();
}

public class LocalizationMiddleware : IMiddleware
{
  public async Task InvokeAsync(HttpContext context, RequestDelegate next)
  {
    var cultureKey = context.Request.Headers["Accept-Language"];
    if (!string.IsNullOrEmpty(cultureKey))
    {
      if (DoesCultureExist(cultureKey))
      {
        var culture = new CultureInfo(cultureKey);
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
      }
    }

    await next(context);
  }

  private static bool DoesCultureExist(string cultureName)
  {
    return CultureInfo.GetCultures(CultureTypes.AllCultures).Any(culture =>
      string.Equals(culture.Name, cultureName, StringComparison.CurrentCultureIgnoreCase));
  }
}
