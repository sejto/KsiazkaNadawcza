Create View Faktury as
select Data, NrDok, Nazwa, Ulica, NrDomu, Kod, Miasto from Dok D
inner join dokkontr DK
on dk.dokid=D.dokid
inner join Kontrahent K
on DK.kontrid=K.kontrid
where typdok=33 and d.aktywny=1
---
select * from faktury where data='2018-02-19'
----------------------
select Data, NrDok, Nazwa, Ulica, NrDomu, Kod, Miasto from Dok D
inner join dokkontr DK
on dk.dokid=D.dokid
inner join Kontrahent K
on DK.kontrid=K.kontrid
where typdok=33 and d.aktywny=1 and data='2018-02-19'
order by nrdok, nazwa
---------------
select kontrid as ID,Nazwa, Ulica, Nrdomu, kod, miasto from OTD.dbo.kontrahent

*************************
SELECT t.nazwa, STUFF((SELECT ',' + s.nrdok FROM Faktury s 
WHERE s.nazwa = t.nazwa and data = '2018-02-19' FOR XML PATH('')),1,1,'') AS CSV 
FROM Faktury AS t where data = '2018-02-19' GROUP BY t.nazwa
************************
SELECT t.nazwa,t.data, STUFF((SELECT ',' + s.nrdok FROM OTD.dbo.Faktury s 
WHERE s.nazwa = t.nazwa and data = '2018-02-19' FOR XML PATH('')),1,1,'') AS CSV,Ulica, NrDomu, Kod, Miasto
FROM OTD.dbo.Faktury AS t where data = '2018-02-19' GROUP BY t.nazwa, t.data, t.ulica, t.nrdomu, t.kod,t.miasto
--------------------
select Nazwa, Ulica,CASE WHEN (NrLokalu IS NULL or NrLokalu='')THEN NrDomu ELSE NrDomu+'/'+NrLokalu END as NrDomu, kod, miasto from OTD.dbo.kontrahent where nazwa like '%oil%'
