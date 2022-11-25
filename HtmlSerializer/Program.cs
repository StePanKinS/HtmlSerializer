using Html;

Tag[]? html = HtmlSerializer.Deserialize(@"C:\Users\Polina\Степка\Html\index.html");


string serialized = HtmlSerializer.Serialize(html);
Console.WriteLine(serialized);