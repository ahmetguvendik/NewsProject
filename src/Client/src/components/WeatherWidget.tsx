import { useEffect, useState } from 'react'
import { fetchWeather, type WeatherNow } from '../api/weather'

/** Dekoratif bir widget — hava durumu alınamazsa sessizce gizlenir, sayfayı bozmaz. */
export function WeatherWidget() {
  const [weather, setWeather] = useState<WeatherNow | null>(null)

  useEffect(() => {
    let cancelled = false

    fetchWeather()
      .then((result) => {
        if (!cancelled) setWeather(result)
      })
      .catch(() => {
        // sessizce gizlenir
      })

    return () => {
      cancelled = true
    }
  }, [])

  if (!weather) return null

  return (
    <span className="weather-chip" title={weather.description}>
      {weather.icon} {weather.city} {weather.temperatureC}°
    </span>
  )
}
