# معماری نوبت

## نمای کلی

نوبت یک سامانه رزرو نوبت آرایشگاه و سالن زیبایی است که با معماری Modular Monolith تمیز ساخته شده است.

## لایههای معماری

### بکاند (ASP.NET Core 10)

| لایه | پروژه | مسئولیت |
| --- | --- | --- |
| دامنه | `Nobat.Domain` | موجودیتها، enumها، رویدادهای دامنه، منطق خالص کسب و کار |
| کاربرد | `Nobat.Application` | موارد استفاده (CQRS با MediatR)، DTOها، اعتبارسنجیها (FluentValidation)، رابطها |
| زیرساخت | `Nobat.Infrastructure` | EF Core DbContext، مخزنها، ASP.NET Identity، JWT، سرویسهای خارجی |
| API | `Nobat.Api` | کنترلرها، middleware، ترکیب وابستگیها، Swagger |

**جریان وابستگی**: `Api → Infrastructure → Application → Domain`

لایه دامنه هیچ وابستگی خارجی ندارد. لایه کاربرد تنها به دامنه وابسته است. لایه زیرساخت رابطهای لایه
کاربرد را پیاده سازی می کند و لایه API همه چیز را کنار هم قرار می دهد.

### فرانتاند (Nuxt 4 + Vue 3 + TypeScript)

- Nuxt 4 با SSR
- Tailwind CSS 4 + Nuxt UI
- Pinia برای مدیریت وضعیت
- تبدیل تقویم جلالی↔میلادی به صورت حسابی در `app/composables/useJalali.ts` پیاده سازی شده
  و از آفست ثابت +03:30 استفاده می کند (ایران از سال ۲۰۲۲ ساعت تابستانی ندارد). از هیچ افزونه
  تقویمی استفاده نمی شود: `jalaliday` در برخی تاریخها یک روز انحراف دارد و جابهجاکردن منطقه زمانی
  یک نمونه آگاهازتقویم، رفتار `.startOf('day')` را خراب می کند. کار با میلیثانیههای epoch نتیجهای
  یکسان در سرور، CI (UTC) و مرورگر تضمین می کند.
- آیکونهای Lucide
- فونت Vazirmatn
- پشتیبانی کامل راست چین
- حالت روشن و تاریک

## طرح دیتابیس

### کاربران (ASP.NET Core Identity)

- Id، UserName (همان شماره موبایل)، FullName، PhoneNumber
- نقش: Admin | Barber | Customer
- LockoutEnd، PhoneNumberConfirmed

ایمیل جمع آوری نمی شود: شماره موبایل شناسه ورود است، بنابراین `UserName` و `PhoneNumber` هر دو همان
عدد ۱۱ رقمی را نگه میدارند و `RequireUniqueEmail` خاموش است.

### سالنها (Salons)

- Id، Name، Slug، Address، Phone، Description
- DefaultTimezone (پیشفرض: Asia/Tehran)
- CreatedAt، UpdatedAt

### آرایشگران (Barbers)

- Id، UserId (FK → Users)، SalonId (FK → Salons)
- FullName، Bio، AvatarUrl، IsActive
- CreatedAt، UpdatedAt

### خدمات (Services)

- Id، SalonId (FK → Salons)
- Name، Description، DurationMinutes، Price
- IsActive، CreatedAt، UpdatedAt

### BarberServices (جدول واسطه (رابطه چند به چند))

- BarberId (FK → Barbers)، ServiceId (FK → Services)
- کلید اصلی: (BarberId، ServiceId)

### ساعات کاری (WorkingHours)

- Id، BarberId (FK → Barbers)
- DayOfWeek (در ایران ۰=شنبه ... ۶=جمعه)
- StartTime (TimeOnly)، EndTime (TimeOnly)
- BreakStart (TimeOnly?)، BreakEnd (TimeOnly?)

### تعطیلات (Holidays)

- Id، SalonId (FK → Salons)
- Date (DateOnly)، Description
- CreatedAt

### نوبتها (Appointments)

- Id، SalonId، BarberId، CustomerId، ServiceId
- StartAt (UTC)، EndAt (UTC)
- وضعیت: Pending | Confirmed | Completed | Cancelled | NoShow
- Notes، CreatedAt، UpdatedAt، CancelledAt
- TimeRange (نوع `tstzrange` تولیدشده که توسط trigger بهروز می شود)

**جلوگیری از تداخل نوبت** — دو لایه، هر دو لازم هستند:

1. **سطح اپلیکیشن** (`CreateAppointmentCommand`): یک کوئری تشخیص تداخل قبل از درج، پاسخ دوستانه
   `409 SLOT_ALREADY_BOOKED` برمیگرداند.
2. **سطح دیتابیس (مرجع نهایی)**: قید انحصاری PostgreSQL:

   ```sql
   ALTER TABLE "Appointments"
     ADD CONSTRAINT "ck_appointments_nooverlap"
     EXCLUDE USING GIST ("BarberId" WITH =, "TimeRange" WITH &&)
     WHERE ("Status" NOT IN (3, 4));   -- Cancelled, NoShow
   ```

   یک trigger فیلد `TimeRange` را با `StartAt`/`EndAt` هماهنگ نگه میدارد و افزونه `btree_gist`
   امکان استفاده از `=` روی ستون UUID داخل ایندکس GiST را فراهم می کند. بنابراین دو مشتری که همزمان
   یک زمان را درخواست میکنند نمیتوانند هر دو موفق شوند: نفر دوم با SQLSTATE `23P01` مواجه می شود
   که هندلر آن را به همان پاسخ `SLOT_ALREADY_BOOKED` تبدیل می کند. لغو نوبت هم زمان را آزاد می کند،
   چون ردیفهای لغوشده خارج از شرط جزئی قید قرار میگیرند.

### تاریخچه وضعیت نوبت (AppointmentStatusHistory)

- Id، AppointmentId (FK → Appointments)
- Status، ChangedByUserId (FK → Users)، CreatedAt (UTC)

## مدیریت زمان

- تمام زمانها به صورت UTC ذخیره می شوند
- زمان کسب و کار از منطقه زمانی `Asia/Tehran` استفاده می کند
- API زمانهای ISO 8601 UTC را میپذیرد و برمیگرداند
- فرانتاند برای نمایش، آنها را به تقویم شمسی تبدیل می کند

## احراز هویت

- ورود با شماره موبایل + رمز عبور (هیچ ایمیلی در کار نیست)
- توکنهای JWT Bearer، توکن دسترسی ۱۵ دقیقهای و توکن تازهسازی چرخشی ۷ روزه
- نقشها: Admin، Barber، Customer
- ASP.NET Core Identity برای مدیریت کاربران

## سازماندهی ماژولها (Application)

```
Nobat.Application/
├── Modules/
│   ├── Identity/        # ثبتنام، ورود، تازهسازی، نمایه
│   ├── Salons/          # CRUD، دادههای صفحه عمومی سالن
│   ├── Barbers/         # CRUD، برنامه آرایشگران
│   ├── Services/        # CRUD
│   ├── Schedules/       # ساعات کاری، تعطیلات
│   └── Appointments/    # رزرو، زمانهای آزاد، وضعیت، تاریخچه
├── Common/              # DTOها، رابطها، behaviorهای مشترک
└── DependencyInjection.cs
```

هر ماژول شامل این قسمتهاست:

- `Commands/` (نوشتن با CQRS از طریق MediatR)
- `Queries/` (خواندن با CQRS)
- `DTOs/`
- `Validators/`
- `Handlers/`

## قراردادهای API

- نشانیهای RESTful
- پیشوند `/api/v1/`
- مدیریت متمرکز خطاها با ProblemDetails (RFC 7807)
- شکل یکسان برای پاسخ خطا
- صفحهبندی با cursor/offset
- مستندات OpenAPI/Swagger
