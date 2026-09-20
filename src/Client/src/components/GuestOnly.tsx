import { Navigate, useLocation } from 'react-router-dom'
import type { ReactNode } from 'react'
import { useAuth } from '../auth/AuthContext'

/**
 * <see cref="Guard"/>'ın tersi: yalnızca oturumu OLMAYANLARA açık sayfalar.
 *
 * Giriş ve kayıt ekranları korumasızdı, dolayısıyla giriş yapmış bir kullanıcı
 * da açabiliyordu. Ortaya çelişkili bir ekran çıkıyordu: üst barda kendi adı ve
 * rolü, menüde Hesabım ve Yeni haber dururken sayfa ona "Kayıt olan herkese
 * okuyucu rolü atanır" ve "Zaten hesabınız var mı?" diyordu.
 *
 * Daha önce bunun kozmetikten öte bir sonucu da vardı: kayıt sayfası başarıdan
 * sonra otomatik giriş yapıyordu, yani admin olarak girmişken farklı bir
 * e-postayla kaydolan kişinin oturumu sessizce yeni okuyucu hesabına dönüyordu.
 * Otomatik giriş kaldırıldı (bkz. RegisterPage), geriye bu tutarsız görüntü
 * kalmıştı.
 *
 * `replace`: geri tuşu kullanıcıyı tekrar buraya atıp döngüye sokmasın.
 */
export function GuestOnly({ children }: { children: ReactNode }) {
  const { session } = useAuth()
  const location = useLocation()

  if (session) {
    // HEDEF, LoginPage'inkiyle AYNI OLMAK ZORUNDA. Guard korumalı bir sayfadan
    // girişe yönlendirirken nereden geldiğini state.from'a yazıyor ve LoginPage
    // giriş başarılı olunca oraya dönüyor. Ama giriş anında önce oturum state'i
    // güncelleniyor: bu bileşen LoginPage'in navigate(from) çağrısından ÖNCE
    // render olup yönlendirmeyi kapıyor. Burada sabit "/" verilseydi, korumalı
    // bir sayfaya girmeye çalışan kullanıcı giriş yaptıktan sonra gitmek
    // istediği yer yerine akışa düşerdi.
    const from = (location.state as { from?: string } | null)?.from
    return <Navigate to={from ?? '/'} replace />
  }

  return <>{children}</>
}
