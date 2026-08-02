import { useEffect, useState } from 'react'
import { fetchAnkaraWeather, type WeatherNow } from '../api/weather'

/** Dekoratif bir widget — Open-Meteo erişilemezse sessizce gizlenir, sayfayı bozmaz. */
export function WeatherWidget() {
  const [weather, setWeather] = useState<WeatherNow | null>(null)

  useEffect(() => {
    let cancelled = false

    fetchAnkaraWeather()
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
    <span className="weather-chip">
      {weather.icon} Ankara {weather.temperatureC}°
    </span>
  )
}
