using System;
class Program {
    static void Main() {
        try {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            Console.WriteLine("Success: " + tz.Id);
        } catch (Exception e) {
            Console.WriteLine("Error: " + e.Message);
        }
    }
}
