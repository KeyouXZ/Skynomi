skynomi auction system

author folvrix
ver 1.0.0
dep skynomi core
if keyou reading this listen the plugin might be buggy since i made in record time and its not really tested by 2 players since i didn't really have time sorry brev
what is this

simple live auction sys for tshock
players auction items bid live no dupes no bs

all server side logged persistent

features

throw to auction
/auction create <price>
server says throw item
u drop it plugin yoinks it instantly logs it no dupe possible

live bids
real time bids
last sec bids extend timer automatically

econ
uses skynomi econ skyorbs
outbid = auto refund

delivery q
inv full or offline item goes delivery q
claim later

db backed
sqlite
auctions + items survive restarts

how to use
start auction

/auction create 500
server prompts
throw item
done broadcast sent

bidding

/auction bid 600
must beat current + min inc
outbid = refunded auto

claim items

if inv full or offline

/auction claim

pulls all queued items

other cmds

/auction list
/auction info <id>
/auction cancel

admin stuff
admin cmds

/auction admin cancel <id>
force cancel item back seller refund bidder

/auction admin end <id>
insta end highest bidder wins

/auction admin list
see all auctions

/auction admin reload
reload config no restart

/auction settings broadcast on
/auction settings broadcast off

perms
perm	desc	rank
auction.create	create auctions	user vip
auction.bid	bid	user
auction.cancel	cancel own	user
auction.claim	claim q	user
auction.settings	toggle broadcast	admin
auction.admin	full access	admin
config

path
tshock/skynomi/auction.json

{
  "Broadcast Auction": true,
  "Auction Duration Seconds": 20,
  "Bid Extension Seconds": 2,
  "Minimum Bid Increment": 1
}
