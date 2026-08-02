// Open-Meteo — API key gerekmiyor, CORS açık, doğrudan tarayıcıdan çağrılabiliyor.
// Şehir sabit: Ankara. Kullanıcı konumu istemiyoruz.

const ANKARA = { latitude: 39.9334, longitude: 32.8597 }

export interface WeatherNow {
  temperatureC: number
  icon: string
}

export async function fetchAnkaraWeather(): Promise<WeatherNow> {
  const url = new URL('https://api.open-meteo.com/v1/forecast')
  url.searchParams.set('latitude', String(ANKARA.latitude))
  url.searchParams.set('longitude', String(ANKARA.longitude))
  url.searchParams.set('current', 'temperature_2m,weather_code')
  url.searchParams.set('timezone', 'Europe/Istanbul')

  const response = await fetch(url)
  if (!response.ok) throw new Error('Hava durumu alınamadı.')

  const body = await response.json()

  return {
    temperatureC: Math.round(body.current.temperature_2m),
    icon: weatherIcon(body.current.weather_code),
  }
}

/** WMO hava durumu kodunu (Open-Meteo'nun döndürdüğü standart) basit bir emojiye çevirir. */
function weatherIcon(code: number): string {
  if (code === 0) return '☀️'
  if (code <= 2) return '🌤️'
  if (code === 3) return '☁️'
  if (code === 45 || code === 48) return '🌫️'
  if (code >= 51 && code <= 57) return '🌦️'
  if (code >= 61 && code <= 67) return '🌧️'
  if (code >= 71 && code <= 77) return '❄️'
  if (code >= 80 && code <= 82) return '🌦️'
  if (code >= 85 && code <= 86) return '🌨️'
  if (code >= 95) return '⛈️'
  return '🌡️'
}
