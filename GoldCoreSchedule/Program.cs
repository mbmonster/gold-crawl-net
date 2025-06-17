using Google.Cloud.Firestore;
using Newtonsoft.Json;
using System.Xml;

string apiUrl = "http://api.btmc.vn/api/BTMCAPI/getpricebtmc?key=3kd8ub1llcg9t45hnoh8hmn7t5kc2v";
string projectId = "binance-79016"; // Thay bằng Project ID của bạn

// Thử lấy từ biến môi trường (GitHub Actions)
string serviceAccountKeyJson = Environment.GetEnvironmentVariable("FIREBASE_SERVICE_ACCOUNT_KEY");

// Nếu không có biến môi trường, đọc từ file local
if (string.IsNullOrEmpty(serviceAccountKeyJson))
{
    string localPath = "./binance-79016-firebase-adminsdk-r8x0o-2a41cbb89d.json";
    if (File.Exists(localPath))
    {
        serviceAccountKeyJson = File.ReadAllText(localPath);
        Console.WriteLine("Đang sử dụng credentials từ file local");
    }
    else
    {
        throw new Exception("Không tìm thấy credentials. Vui lòng kiểm tra biến môi trường FIREBASE_SERVICE_ACCOUNT_KEY hoặc file local credentials.json");
    }
}

// Tạo file tạm thời để lưu credentials
string tempPath = Path.GetTempFileName();
File.WriteAllText(tempPath, serviceAccountKeyJson);

try 
{
    // Khởi tạo Firestore với credentials từ file tạm
    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", tempPath);
    FirestoreDb db = FirestoreDb.Create(projectId);

    using (HttpClient client = new HttpClient())
    {
        try
        {
            // Lấy dữ liệu từ API
            HttpResponseMessage response = await client.GetAsync(apiUrl);
            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();

                // Xử lý dữ liệu
                var processedData = ProcessData(responseBody);

                // Lưu vào Firestore
                string documentId = DateTime.Now.ToString("yyyyMMdd");
                DocumentReference docRef = db.Collection("gold").Document(documentId);

                await docRef.SetAsync(processedData);
            }
            else
            {
                Console.WriteLine($"Lỗi: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi: {ex.Message}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Lỗi: {ex.Message}");
}

static dynamic ProcessData(string rawJson)
{
    var rawData = JsonConvert.DeserializeObject<dynamic>(rawJson);
    var dataList = rawData.DataList.Data;
    var currentDay = DateTime.Now.ToString("yyyyMMdd");

    // Hàm normalizeItem
    Func<dynamic, dynamic> normalizeItem = (item) =>
    {
        var resultData = new Dictionary<string, dynamic>();
        foreach (var prop in item)
        {
            string key = prop.Name;
            if (key.Contains("@n_")) resultData["name"] = prop.Value;
            if (key.Contains("@k_")) resultData["karat"] = prop.Value;
            if (key.Contains("@h_")) resultData["purity"] = prop.Value;
            if (key.Contains("@pb_")) resultData["buyPrice"] = prop.Value;
            if (key.Contains("@ps_")) resultData["sellPrice"] = prop.Value;
            if (key.Contains("@pt_")) resultData["diff"] = prop.Value;
            if (key.Contains("@d_")) resultData["updatedAt"] = prop.Value;
            if (key == "@row") resultData["row"] = prop.Value;
        }
        return resultData;
    };

    // Hàm filterAndSort
    Func<List<dynamic>, string, dynamic> filterAndSort = (item, type) =>
    {
        var filtered = item
            .Select(normalizeItem)
            .Where(obj =>
                obj["name"]?.ToString().Contains(type) == true &&
                obj["karat"]?.ToString() == "24k" &&
                obj["purity"]?.ToString() == "999.9" &&
                !string.IsNullOrEmpty(obj["updatedAt"]?.ToString()))
            .OrderByDescending(obj => DateTime.ParseExact(
                obj["updatedAt"].ToString(),
                "dd/MM/yyyy HH:mm",
                System.Globalization.CultureInfo.InvariantCulture
            ))
            .FirstOrDefault();
        return filtered;
    };

    // Xử lý dữ liệu
    var result = filterAndSort(dataList.ToObject<List<dynamic>>(), "VÀNG MIẾNG SJC");
    var result1 = filterAndSort(dataList.ToObject<List<dynamic>>(), "NHẪN TRÒN TRƠN");
    var result2 = filterAndSort(dataList.ToObject<List<dynamic>>(), "TRANG SỨC BẰNG");
    var result3 = filterAndSort(dataList.ToObject<List<dynamic>>(), "VÀNG NGUYÊN LIỆU");

    // Tạo danh sách items với định dạng mới
    var items = new List<dynamic>
            {
                new
                {
                    buyPrice = result?["buyPrice"]?.ToString(),
                    diff = result?["diff"]?.ToString(),
                    id = currentDay,
                    karat = result?["karat"]?.ToString(),
                    name = result?["name"]?.ToString(),
                    purity = result?["purity"]?.ToString(),
                    row = result?["row"]?.ToString(),
                    sellPrice = result?["sellPrice"]?.ToString(),
                    type = 1,
                    updatedAt = result?["updatedAt"]?.ToString()
                },
                new
                {
                    buyPrice = result1?["buyPrice"]?.ToString(),
                    diff = result1?["diff"]?.ToString(),
                    id = currentDay,
                    karat = result1?["karat"]?.ToString(),
                    name = result1?["name"]?.ToString(),
                    purity = result1?["purity"]?.ToString(),
                    row = result1?["row"]?.ToString(),
                    sellPrice = result1?["sellPrice"]?.ToString(),
                    type = 2,
                    updatedAt = result1?["updatedAt"]?.ToString()
                },
                new
                {
                    buyPrice = result2?["buyPrice"]?.ToString(),
                    diff = result2?["diff"]?.ToString(),
                    id = currentDay,
                    karat = result2?["karat"]?.ToString(),
                    name = result2?["name"]?.ToString(),
                    purity = result2?["purity"]?.ToString(),
                    row = result2?["row"]?.ToString(),
                    sellPrice = result2?["sellPrice"]?.ToString(),
                    type = 3,
                    updatedAt = result2?["updatedAt"]?.ToString()
                },
                new
                {
                    buyPrice = result3?["buyPrice"]?.ToString(),
                    diff = result3?["diff"]?.ToString(),
                    id = currentDay,
                    karat = result3?["karat"]?.ToString(),
                    name = result3?["name"]?.ToString(),
                    purity = result3?["purity"]?.ToString(),
                    row = result3?["row"]?.ToString(),
                    sellPrice = result3?["sellPrice"]?.ToString(),
                    type = 4,
                    updatedAt = result3?["updatedAt"]?.ToString()
                }
            };

    var processedData = new
    {
        items = items
    };

    return processedData;
}