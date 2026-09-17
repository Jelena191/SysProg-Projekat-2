## Zadatak 20
Kreirati Web server koji klijentu omogućava prikaz vrednosti zagađenja vazduha uz pomoć IQ Air 
API-a. Pretraga se može vršiti pomoću filtera koji se definišu u okviru query-a. Vrednosti 
zagađenja vazduha se vraćaju kao odgovor (pretragu vršiti po gradu). Svi zahtevi serveru se šalju 
preko browser-a korišćenjem GET metode. Ukoliko navedene vrednosti zagađenja ne postoje, 
prikazati grešku klijentu.   
Način funkcionisanja IQ Air API-a je moguće proučiti na sledećem linku: https://api
docs.iqair.com/?version=latest 
Primer poziva serveru:    
http://api.airvisual.com/v2/city?city=LosAngeles&state=California&country=USA&key={{YO
UR_API_KEY}} 

Strategija upravljanja keš memorijom: Vremensko isticanje
### Jelena Krstić 18220