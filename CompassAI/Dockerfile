# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY *.csproj ./
RUN dotnet restore

COPY . .
# عمل Build للمشروع
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# نسخ مخرجات الـ Publish
COPY --from=build /app/publish .

# --- التعديل الجوهري هنا ---
# التأكد من نسخ فولدر الـ Templates يدوياً لو الـ publish متجاهله
# ده بيضمن إن المسار /app/Templates يكون موجود دايماً
COPY --from=build /src/Templates ./Templates

RUN mkdir -p /app/wwwroot/users

# تعطيل التحذير بتاع الـ HTTPS جوه الكونتينر
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CompassAI.dll"]