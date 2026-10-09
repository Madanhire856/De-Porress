const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const methods = [{id:3,name:'Cash',values:[300,0]}, {id:2,name:'Bank',values:[500,50]}, {id:5,name:'Swipe',values:[250,0]}];
const canvas = {dataset:{currency:'USD', labels:JSON.stringify(['07 Oct','08 Oct']), dates:JSON.stringify(['2026-10-07','2026-10-08']), methods:JSON.stringify(methods), reversed:JSON.stringify([100,0])}};
let config, destination;
vm.runInNewContext(fs.readFileSync('SMS.Web/wwwroot/js/finance-dashboard.js','utf8'),{
 Chart:function(target,options){assert.equal(target,canvas);config=options;},
 document:{getElementById:id=>id==='finance-trend-chart'?canvas:null},
 window:{matchMedia:()=>({matches:true}),location:{href:'https://school.example/?TrendFrom=2026-10-01&TrendTo=2026-10-08',assign:url=>destination=url}}, URL
});
assert.equal(config.type,'bar');
assert.equal(config.options.scales.x.stacked,true);
assert.equal(config.options.scales.y.stacked,true);
assert.equal(config.options.interaction.mode,'index');
assert.equal(config.data.datasets[2].backgroundColor,'#9563b5');
assert.equal(config.data.datasets[0].data[1],0);
// Full daily total must include methods hidden through the legend.
const footer=Array.from(config.options.plugins.tooltip.callbacks.footer([{dataIndex:0}]));
assert.deepEqual(footer,['Daily total: USD 1,050.00','Reversed: USD 100.00','Net: USD 950.00']);
config.options.onClick(null,[{index:1}]);
const url=new URL(destination);assert.equal(url.searchParams.get('CollectionDate'),'2026-10-08');
assert.equal(url.searchParams.get('TrendFrom'),'2026-10-01');
config.options.onClick(null,[]);assert.equal(new URL(destination).searchParams.get('CollectionDate'),'2026-10-08');
assert.equal(config.options.animation,false);
console.log('PASS: stacked methods, zero days, full daily totals, reversals, click date preservation and reduced motion.');
