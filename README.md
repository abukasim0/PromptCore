# PromptCore — مولّد برومبتات الذكاء الاصطناعي

تطبيق سطح مكتب (WPF / .NET 10) لتوليد وتنظيم برومبتات الذكاء الاصطناعي،
بثيمين داكن/فاتح، وواجهة عربية/إنجليزية، ومكتبة محفوظات بـ SQLite.

- **الإصدار:** 1.1.0
- **المنصة:** Windows (net10.0-windows)

## 👤 المطور والتواصل

| | |
|---|---|
| 👤 **تطوير** | Abu Kasim |
| 🐙 **جيت هاب** | [github.com/abukasim0](https://github.com/abukasim0) |
| 📢 **قناة التليجرام** | [t.me/programs_edditing](https://t.me/programs_edditing) |
| 🌐 **المدونة الرسمية** | [filefreesy.blogspot.com](https://filefreesy.blogspot.com) |
| 🔗 **كافة الروابط** | [linktr.ee/abukasim](https://linktr.ee/abukasim) |
| 💬 **للتواصل المباشر** | [@Abu_Kasim](https://t.me/Abu_Kasim) |

## التشغيل من المصدر

```bat
dotnet test          :: 37 اختبار
dotnet run --project src\PromptCore.UI
```

- قاعدة البيانات: `%AppData%\PromptCore\promptcore.db` (تُنشأ وتُرحَّل تلقائياً؛
  نسخة `PromptForge` القديمة تُنسخ تلقائياً عند أول تشغيل).

## تصدير ملف EXE

```bat
:: نسخة خفيفة (~3MB) — تتطلب .NET 10 Desktop Runtime على الجهاز
dotnet publish src\PromptCore.UI -c Release -r win-x64 --self-contained false ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

:: نسخة مستقلة (~66MB) — تعمل على أي جهاز Windows 10/64bit بدون متطلبات
dotnet publish src\PromptCore.UI -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true
```

الناتج في `src\PromptCore.UI\bin\Release\net10.0-windows\win-x64\publish\PromptCore.exe`
(الأيقونة والبيانات مدمجة داخل الملف).
