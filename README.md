**Live Demo:** Μπορείτε να δοκιμάσετε το bot στέλνοντας /setup εδώ: [t.me/Timlog_Pi5_bot](https://t.me/Timlog_Pi5_bot)
Τρέχει σε test environment και θα χρειαστείτε credentials από την ΑΑΔΕ από το βήμα 2

## Use Cases (Παραδειγματικά Scenarios Χρήσης)

Ακολουθούν παραδείγματα για το πώς μπορείς να χρησιμοποιήσεις το Timlog μέσω του Telegram bot, μαζί με το αντίστοιχο αναμενόμενο αποτέλεσμα:

- **B2B Εσωτερικού (Κατηγορία 2.1 - Τιμολόγιο Παροχής):**
  - *Prompt χρήστη:* "Κόψτε τιμολόγιο 150 ευρώ συν ΦΠΑ στην εταιρεία Alpha Tech Α.Ε. για υπηρεσίες συμβουλευτικής ανάπτυξης."
  - *Αναμενόμενο αποτέλεσμα:* Το bot επιστρέφει μήνυμα προεπισκόπησης (preview) με τα αναλυτικά στοιχεία και τα ποσά. Μετά την έγκρισή σου, εμφανίζεται μήνυμα επιτυχίας μαζί με το επίσημο σύνδεσμο (link) και το QR code του παραστατικού από το myDATA.

- **B2B Ενδοκοινοτική Παροχή (Κατηγορία 2.2):**
  - *Prompt χρήστη:* "Τιμολόγιο 500 USD για cloud migration support στη Beta Software SRL στη Γαλλία."
  - *Αναμενόμενο αποτέλεσμα:* Εμφανίζεται preview που κατατάσσει αυτόματα τη συναλλαγή στον τύπο 2.2. Μετά την επιβεβαίωσή σου, εκδίδεται το παραστατικό μέσω του API και λαμβάνεις το αντίστοιχο link και QR code.

- **B2B σε Τρίτη Χώρα (Κατηγορία 2.3):**
  - *Prompt χρήστη:* "Έκδοση invoice 1200 δολαρίων για code review στην Delta LLC στις ΗΠΑ."
  - *Αναμενόμενο αποτέλεσμα:* Το σύστημα αναγνωρίζει τη χώρα εκτός ΕΕ, προτείνει τον τύπο 2.3 στο preview και, με το πάτημα της έγκρισης, πραγματοποιεί την υποβολή επιστρέφοντας το τελικό link και το QR code.

# Οδηγός Εγκατάστασης Timlog
---

## 1. Δημιουργία Telegram Bot

1. Άνοιξε το **Telegram** και ξεκίνα μια συνομιλία με τον **[@BotFather](https://t.me/BotFather)**.
2. Στείλε την εντολή για δημιουργία νέου bot:
   /newbot

3. Δώσε ένα όνομα εμφάνισης (Display Name), π.χ. `Timlog Bot`.
4. Δώσε ένα μοναδικό username που να καταλήγει σε `bot`, π.χ. `timlog_test_bot`.
5. Αποθήκευσε το HTTP API Token που θα σου δώσει σε ασφαλές μέρος:
   ΤΟ_TELEGRAM_TOKEN_ΣΟΥ
---

## 2. Εγγραφή στο myDATA της ΑΑΔΕ (Dev/Test Περιβάλλον)

1. Μπες στο **[myDATA Dev Registration Portal](https://mydata-dev-register.azurewebsites.net/)**.
2. Πάτα **Sign up** για να δημιουργήσεις έναν developer λογαριασμό.
3. Μόλις συνδεθείς, πήγαινε στο προφίλ σου. Βρες και αντίγραψε το **User ID** καθώς και το **Subscription Key** (Primary ή Secondary).

---

## 3. Ρύθμιση Περιβάλλοντος (.env)

Κατέβασε τον κώδικα στον υπολογιστή σου και μπες στον φάκελο:
```bash
git clone https://github.com/your-username/Timlog.git
cd Timlog
```

Δημιούργησε ένα αρχείο με όνομα `.env` στον κεντρικό φάκελο:
```bash
touch .env
```

Συμπλήρωσε το αρχείο με τα δικά σου στοιχεία:

| Μεταβλητή | Προέλευση | Παράδειγμα / Placeholder |
| :--- | :--- | :--- |
| **TELEGRAM_BOT_TOKEN** | BotFather | `ΤΟ_TELEGRAM_TOKEN_ΣΟΥ` |
| **TUNNEL_TOKEN** | Cloudflare Dashboard | `ΤΟ_CLOUDFLARE_TOKEN_ΣΟΥ` |

Παράδειγμα αρχείου `.env`:
```env
TELEGRAM_BOT_TOKEN=ΤΟ_TELEGRAM_TOKEN_ΣΟΥ
TUNNEL_TOKEN=ΤΟ_CLOUDFLARE_TOKEN_ΣΟΥ
```

---

## 4. Επιλογές Εκτέλεσης (Docker Profiles)

Διάλεξε **μόνο ένα** από τα παρακάτω προφίλ, ανάλογα με τις ανάγκες σου.

### Επιλογή Α: Προσωρινό Τούνελ (Cloudflare Quick Tunnel)
*Ιδανικό αν δεν έχεις δικό σου domain.*

1. Ξεκίνα τα containers:
   docker compose --profile temp up -d --build

2. Δες τα logs για να βρεις το προσωρινό URL:
   docker logs timlog_temp_tunnel

3. Δήλωσε το Webhook στο Telegram (κράτα το `.trycloudflare.com` από τα logs):
   ```bash
   curl -X POST "https://api.telegram.org/botΤΟ_TELEGRAM_TOKEN_ΣΟΥ/setWebhook?url=https://ΤΟ_URL_ΣΟΥ.trycloudflare.com/api/telegram/webhook"
   ```

### Επιλογή Β: Μόνιμο Τούνελ (Cloudflare Named Tunnel)
*Ιδανικό αν έχεις ήδη δικό σου domain ρυθμισμένο.*

1. Ξεκίνα τα containers:
   ```bash
   docker compose --profile tunnel up -d --build
   ```

2. Δήλωσε το Webhook με το πραγματικό σου domain:
   ```bash
   curl -X POST "https://api.telegram.org/botΤΟ_TELEGRAM_TOKEN_ΣΟΥ/setWebhook?url=https://ΤΟ_DOMAIN_ΣΟΥ/api/telegram/webhook"
   ```

---

## 5. Παρακολούθηση & Τερματισμός

Για να δεις ζωντανά αν τα αιτήματα φτάνουν σωστά στην εφαρμογή:
```bash
docker compose logs api -f
```
Στείλε ένα μήνυμα (π.χ. `/start`) στο bot σου. Θα δεις το POST request να εμφανίζεται άμεσα στο τερματικό.

Για να κλείσεις τα containers:
```bash
docker compose --profile temp down
# ή
docker compose --profile tunnel down
```

## 6. Aspire Dashboard

Το project περιλαμβάνει ενσωματωμένο το .NET Aspire Dashboard

Για να δεις τα logs εκκίνησης του dashboard (όπου περιέχεται και το μοναδικό token σύνδεσης), τρέξε:
docker logs timlog_aspire_dashboard -f

Αφού ξεκινήσεις τα containers, άνοιξε τον browser σου και μπες στο Login URL με το token σου (παράδειγμα):
http://localhost:18888/login?t=fee63e66faf33c214c53c3f75be06153

Μέσα από το UI μπορείς να παρακολουθείς:
- Structured Logs: Όλα τα logs του API σε ενιαία μορφή με δυνατότητα φιλτραρίσματος.
- Traces: Το ακριβές μονοπάτι και τους χρόνους εκτέλεσης κάθε εισερχόμενου webhook call.
- Metrics: Τη χρήση πόρων, CPU/Memory καθώς και μετρικές δικτύου και HTTP requests.
