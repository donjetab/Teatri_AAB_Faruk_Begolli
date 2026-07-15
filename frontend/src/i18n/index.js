import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'

const storedLanguage = localStorage.getItem('aab-theatre-language')
const initialLanguage = storedLanguage === 'en' ? 'en' : 'sq'

i18n.use(initReactI18next).init({
  fallbackLng: 'sq',
  lng: initialLanguage,
  interpolation: {
    escapeValue: false,
  },
  resources: {
    sq: {
      translation: {
        brand: 'Teatri AAB "Faruk Begolli"',
        nav: {
          home: 'Ballina',
          about: 'Për Ne',
          shows: 'Shfaqjet',
          news: 'Lajme',
          pitf: 'PITF',
          gallery: 'Galeria',
          contact: 'Kontakti',
          reserveNow: 'Rezervo Tani',
        },
        footer: {
          links: 'Linqe',
          visit: 'Vizito',
          follow: 'Na Ndiq',
          newsletter: 'Newsletter',
          newsletterText: 'njoftohu i pari për shfaqjet më të fundit',
          location: 'Lokacioni',
        },
        newsletter: {
          emailLabel: 'Email-i juaj',
          placeholder: 'Email-i juaj',
          submit: 'Dërgo emailin',
          success: 'Faleminderit. U regjistruat me sukses.',
          error: 'Regjistrimi nuk u krye. Provoni përsëri.',
        },
        home: {
          loadError: 'Faqja nuk u ngarkua. Kontrolloni që API është duke punuar.',
          viewProgram: 'Shiko programin',
          aboutTitle: 'Për Ne',
          aboutImage: 'Pamje nga teatri',
          viewAllShows: 'Shiko të gjitha shfaqjet',
          noShows: 'Nuk ka shfaqje të ardhshme për momentin.',
          directedBy: 'Regj. {{director}}',
          reserveForShow: 'Rezervo për {{title}}',
          showPosterAlt: 'Posteri i shfaqjes {{title}}',
          pitfProgram: 'Programi PITF',
          reserveTicket: 'Rezervo biletën',
          reservationTitleFallback: 'Bëhu pjesë e historisë tonë',
          reservationTextFallback: 'Rezervo vendin tënd për shfaqjen e radhës.',
          upcomingShows: {
            line1: 'Shfaqjet e',
            line2: 'ardhshme',
          },
          stats: {
            founded: 'Themelimi',
            performances: 'Performanca',
            spectators: 'Spektatorë',
          },
        },
        months: {
          jan: 'Jan',
          feb: 'Shk',
          mar: 'Mar',
          apr: 'Pri',
          may: 'Maj',
          jun: 'Qer',
          jul: 'Kor',
          aug: 'Gus',
          sep: 'Sht',
          oct: 'Tet',
          nov: 'Nën',
          dec: 'Dhj',
        },
        shell: {
          title: 'Faqja do të ndërtohet në hapin tjetër.',
        },
        states: {
          loading: 'Duke u ngarkuar...',
          error: 'Diçka shkoi keq.',
          empty: 'Nuk ka të dhëna për t’u shfaqur.',
        },
        a11y: {
          skipToContent: 'Kalo te përmbajtja',
          primaryNavigation: 'Navigimi kryesor',
          mobileNavigation: 'Navigimi mobil',
          languageSwitcher: 'Ndrysho gjuhën',
          menu: 'Hap menynë',
        },
      },
    },
    en: {
      translation: {
        brand: 'AAB Theatre "Faruk Begolli"',
        nav: {
          home: 'Home',
          about: 'About',
          shows: 'Shows',
          news: 'News',
          pitf: 'PITF',
          gallery: 'Gallery',
          contact: 'Contact',
          reserveNow: 'Reserve Now',
        },
        footer: {
          links: 'Links',
          visit: 'Visit',
          follow: 'Follow Us',
          newsletter: 'Newsletter',
          newsletterText: 'be the first to hear about the latest shows',
          location: 'Location',
        },
        newsletter: {
          emailLabel: 'Your email',
          placeholder: 'Your email',
          submit: 'Submit email',
          success: 'Thank you. You have subscribed successfully.',
          error: 'Subscription failed. Please try again.',
        },
        home: {
          loadError: 'The homepage could not load. Check that the API is running.',
          viewProgram: 'View program',
          aboutTitle: 'About',
          aboutImage: 'Theatre view',
          viewAllShows: 'View all shows',
          noShows: 'There are no upcoming shows right now.',
          directedBy: 'Dir. {{director}}',
          reserveForShow: 'Reserve for {{title}}',
          showPosterAlt: 'Poster for {{title}}',
          pitfProgram: 'PITF program',
          reserveTicket: 'Reserve ticket',
          reservationTitleFallback: 'Become part of our history',
          reservationTextFallback: 'Reserve your seat for the next performance.',
          upcomingShows: {
            line1: 'Upcoming',
            line2: 'shows',
          },
          stats: {
            founded: 'Founded',
            performances: 'Performances',
            spectators: 'Spectators',
          },
        },
        months: {
          jan: 'Jan',
          feb: 'Feb',
          mar: 'Mar',
          apr: 'Apr',
          may: 'May',
          jun: 'Jun',
          jul: 'Jul',
          aug: 'Aug',
          sep: 'Sep',
          oct: 'Oct',
          nov: 'Nov',
          dec: 'Dec',
        },
        shell: {
          title: 'The page body will be built in the next step.',
        },
        states: {
          loading: 'Loading...',
          error: 'Something went wrong.',
          empty: 'There is nothing to show yet.',
        },
        a11y: {
          skipToContent: 'Skip to content',
          primaryNavigation: 'Primary navigation',
          mobileNavigation: 'Mobile navigation',
          languageSwitcher: 'Change language',
          menu: 'Open menu',
        },
      },
    },
  },
})

document.documentElement.lang = initialLanguage

export default i18n
