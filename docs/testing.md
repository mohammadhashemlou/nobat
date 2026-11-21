# تستها

پروژه سه لایه تست دارد. هر سه لایه در CI هم اجرا می شوند (`.github/workflows/ci.yml`).

| لایه | ابزار | محل | تعداد |
| --- | --- | --- | --- |
| تست واحد دامنه و کاربرد | xUnit | `backend/tests/Nobat.Domain.Tests`، `backend/tests/Nobat.Application.Tests` | ۷۷ |
| تست یکپارچه API | xUnit + `WebApplicationFactory` + PostgreSQL | `backend/tests/Nobat.Api.Tests` | ۳۷ |
| تست واحد/کامپوننت front-end | Vitest + `@vue/test-utils` | `frontend/tests/unit` | ۵۵ |
| End-to-end | Playwright | `frontend/tests/e2e` | ۱۵ |

## Back-end

```bash
cd backend
dotnet test                       # همه تستها
dotnet test tests/Nobat.Domain.Tests        # فقط قوانین خالص دامنه
dotnet test tests/Nobat.Application.Tests   # موتور زمانبندی + اعتبارسنجیها
dotnet test tests/Nobat.Api.Tests           # به PostgreSQL نیاز دارد
```

### تستهای یکپارچه و database

`Nobat.Api.Tests` خط لوله واقعی API را در برابر یک **database یکبارمصرف** راهاندازی می کند:

- در ابتدا یک database با نام منحصربهفرد (`nobat_test_<random>`) ساخته و در پایان حذف می شود؛
- رشته اتصال ابتدا از `NOBAT_TEST_DB`، سپس `ConnectionStrings__Default` و در نهایت از پیشفرض
  محلی توسعه (`localhost:5433`، کاربر `nobat`) خوانده می شود؛
- کاربر database به مجوزهای `CREATE DATABASE` و `CREATE EXTENSION btree_gist` نیاز دارد
  (قید انحصاری به آن وابسته است).

اگر PostgreSQL در دسترس نباشد، تستها **رد می شوند** (skip) با یک پیام توضیحی، نه اینکه شکست بخورند؛
بنابراین `dotnet test` روی ماشینی بدون database هم قابل استفاده است. در CI سرویس `postgres:17` فراهم
است، پس تستها همیشه اجرا می شوند.

```bash
# Point the suite at a specific server
NOBAT_TEST_DB="Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres" \
  dotnet test tests/Nobat.Api.Tests
```

### تستهای یکپارچه چه چیزی را اثبات میکنند

- ثبتنام، ورود و تازهسازی چرخشی توکن (توکن قدیمی باطل می شود)؛
- خطاهای اعتبارسنجی فارسی و مربوط به هر فیلد، به جای پیامهای پیشفرض انگلیسی Identity؛
- جریان کامل رزرو: زمانهای آزاد → رزرو → ناپدید شدن زمان → لغو → بازگشت زمان؛
- تمام مسیرهای رد شدن (`PAST_TIME`، `HOLIDAY`، `OUTSIDE_WORKING_HOURS`، `BREAK_OVERLAP`،
  `NOT_WORKING_DAY`، `SLOT_ALREADY_BOOKED`، `FORBIDDEN`)؛
- **جلوگیری از تداخل در سطح database**: ردیفها مستقیم با SQL و با دور زدن بررسی اپلیکیشن درج
  می شوند و PostgreSQL آنها را با `23P01` روی `ck_appointments_nooverlap` رد می کند؛
- محدودسازی بر اساس نقش (مشتری فقط نوبتهای خودش را میبیند، نمی تواند وضعیت را تغییر دهد یا خدمت
  ایجاد کند).

## Front-end

```bash
cd frontend
npm run test          # Vitest، یکبار اجرا
npm run test:watch    # Vitest، حالت تماشا
```

تستهای واحد پوشش می دهند:

- `useJalali` — تبدیل تقویم جلالی↔میلادی در برابر نقاط مرجع معتبر، رفت و برگشت دهساله، طول ماهها،
  سالهای کبیسه، ارقام فارسی، قالببندی ساعت ۲۴ ساعته و کمککنندههای تاریخ API؛
- `normalizeError` — تبدیل ProblemDetails به یک پیام واحد فارسی؛
- `StatusBadge` — نگاشت هر وضعیت به برچسب و رنگ فارسی درست.

> **چرا منطق تقویم دستنویس است؟** کتابخانه `jalaliday` در برخی تاریخها یک روز انحراف دارد
> (مثلاً ۲۰۲۴-۰۱-۰۵ را ۱۴۰۲/۱۰/۱۶ نمایش می دهد نه ۱۴۰۲/۱۰/۱۵) و فراخوانی `.startOf('day')` روی
> یک نمونه جلالی که منطقه زمانیش جابهجا شده، تاریخ را خراب می کند. به همین دلیل این composable
> هر دو جهت تبدیل را به صورت حسابی از میلیثانیههای epoch انجام می دهد و اصلاً به افزونهای تکیه
> نمی کند.
>
> این تستها همچنین **مستقل از منطقه زمانی میزبان**اند و باید در UTC (همانطور که CI اجرایشان
> می کند)، در تهران و هر جای دیگری پاس بدهند. قبلاً فقط روی ماشین توسعه پاس میشدند چون آن ماشین
> تصادفاً در ایران بود. سریعترین راه برای بازتولید یک باگ منطقه زمانی:
> `TZ=UTC npm run test`

```bash
TZ=UTC npm run test
TZ=America/New_York npm run test
```

## End-to-end

Playwright رابط کاربری واقعی Nuxt را در برابر API واقعی هدایت می کند.

```bash
cd frontend
npx playwright install chromium   # یکبار

# Both servers must be running (API on :5080, Nuxt on :3000)...
npm run test:e2e

# ...or let Playwright build and serve the front-end against a running API
NUXT_PUBLIC_API_BASE=http://localhost:5080 npx playwright test

# Against an already-deployed environment
E2E_BASE_URL=https://staging.example.com npx playwright test
```

وقتی Playwright مسئول front-end است، دستور `npm run build` را اجرا و **خروجی عملیاتی** را سرو
می کند، نه سرور توسعه. این موضوع مهم است: سرور توسعه صدها ماژول ES بستهبندینشده را ارسال می کند،
بنابراین روی یک runner سرد CI ممکن است بسته سمت مشتری دیرتر از زمان انتظار عملیات برسد و تستها
با نشانهگذاری سروری تعامل کنند که هنوز توسط Vue hydrate نشده است. تست کردن خروجی واقعی، زمان
hydrate را زیر یک ثانیه نگه میدارد و دقیقاً همان چیزی است which منتشر می شود.

از آنجایی که hydrate شدن همچنان ناهمزمان است، فایل `tests/e2e/helpers.ts` تابع `gotoApp()` را
ارائه می دهد: ابتدا به صفحه میرود و سپس منتظر `html[data-hydrated="true"]` میماند (که توسط
`app/plugins/hydration.client.ts` تنظیم می شود). همیشه از آن به جای `page.goto` خام استفاده کنید:
پیش از hydrate شدن، کلیکها شنوندهای ندارند و متنی که در اینپوت `v-model` تایپ می شود، هنگام اولین
رندر سمت مشتری از بین میرود.

هر اجرا خدمت، آرایشگر و ساعات کاری خودش را از طریق API میسازد و یک مشتری تازه ثبتنام می کند،
بنابراین اجراها با هم تداخل نمیکنند. این مجموعه تستها تأکید می کند که جریان رزرو **هیچ خطایی در
کنسول مرورگر** ایجاد نمی کند.

## پوشش تست

```bash
cd backend
dotnet test --collect:"XPlat Code Coverage"
```
