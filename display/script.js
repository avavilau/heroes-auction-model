let colors = [ "red", "green","blue","orange","brown"];
let money_slider = document.getElementById("money_range");
let prob_slider = document.getElementById("prob_range");
let money_edit = document.getElementById("money_edit");
let prob_edit = document.getElementById("prob_edit");
money_edit.innerHTML = money_slider.value;
prob_edit.innerHTML = prob_slider.value;

let wins = {}, lose = {};
for (let i = 0; i < data_ponints.length; ++i) {
  if (data_ponints[i].redWin) {
    if (!wins[data_ponints[i].red])
      wins[data_ponints[i].red] = {};
    if (!wins[data_ponints[i].red][data_ponints[i].blue])
      wins[data_ponints[i].red][data_ponints[i].blue] = 0;
    ++wins[data_ponints[i].red][data_ponints[i].blue];
  } else {
    if (!lose[data_ponints[i].red])
      lose[data_ponints[i].red] = {};
    if (!lose[data_ponints[i].red][data_ponints[i].blue])
      lose[data_ponints[i].red][data_ponints[i].blue] = 0;
    ++lose[data_ponints[i].red][data_ponints[i].blue];
  }
}

let xValues = [];
for (let x = 0; x <= 20000; x += 100) {
  xValues.push(x);
}

let prob_table = createTable("probabilities");
let money_table = createTable('moneys');

let canvas = document.createElement('canvas');
canvas.id = "chart";
canvas.style = "width:100%;max-width:1000px;max-height: 500;";
document.body.appendChild(canvas);


let ch = new Chart("chart", {
  type: "line",
  data: {
    labels: xValues,
    datasets: []
  },    
  options: {
    legend: {display: false},
    title: {
      display: true,
      fontSize: 16,
      showLines: null      
    },
    scales: {
      x: {
          type: 'linear',
          ticks: {
          maxRotation: 0
          }
      },
      y: {
          scaleLabel: {
          display: true,
          labelString: 'value'
          }
      }
      },    
    plugins: {
      zoom: {
          zoom: {
          enabled: true,
          drag: {
              animationDuration: 1000
          },
          mode: 'x',
          speed: 0.05
          }
      }
    }     
  }
});

function updateProbabilities() {
  let v = Number.parseFloat(money_slider.value);
  money_edit.value = v;
  let m = (v - 10000) / 10000;

  index = ch.data.datasets.findIndex(x => x.borderColor == "black");
  if (index != -1) {
    ch.data.datasets.splice(index, 1);
  }
  let sliiderLine = {
    fill: false,
    pointRadius: 1,
    borderColor: "black",
    data: [{'x':v, 'y':0}, {'x':v, 'y':1}],
    borderWidth: 1,
    label: 'slider',
    animation: {
      duration: 0
    }    
  };
  ch.data.datasets.push(sliiderLine);
  ch.update();
  
  for (const [red, value1] of Object.entries(towns)) {
    for (const [blue, value2] of Object.entries(towns)) {

      let r0 = coefficients[red + "_0"];
      let r1 = coefficients[red + "_1"];
      let b0 = coefficients[blue + "_0"];
      let b1 = coefficients[blue + "_1"];

      let id = "probabilities_" + red + "_" + blue;

      let w = wins[red][blue];
      let l = lose[red][blue];

      let cell =  document.getElementById(id);
      cell.innerHTML = (1 / (1 + Math.exp(-(r0-b0)-(r1+b1)*m))).toFixed(3) + " (" + w + "|" + l +")";
    }
  }  
}

function updateMoneys() {
  let v = Number.parseFloat(prob_slider.value);
  prob_edit.value = v;

  for (const [red, value1] of Object.entries(towns)) {
    for (const [blue, value2] of Object.entries(towns)) {

      let r0 = coefficients[red + "_0"];
      let r1 = coefficients[red + "_1"];
      let b0 = coefficients[blue + "_0"];
      let b1 = coefficients[blue + "_1"];

      let id = "moneys_" + red + "_" + blue;

      let m = (Math.log((1 / v) - 1) + (r0 - b0)) / (-(r1 + b1));
      m = (m + 1) * 10000;
      m = Math.round(m / 100) * 100;

      let cell =  document.getElementById(id);
      cell.innerHTML = m;
    }
  }  
}

function updateMoneyEidt() {
  let v = money_edit.value;
  v = Math.round(v / 100) * 100; 
  money_slider.value = v;
  updateProbabilities();
}

function updateProbEidt() {
  let v = prob_edit.value;
  v = Math.round(v / 0.1) * 0.1; 
  prob_slider.value = v;
  updateMoneys();
}

money_slider.oninput = updateProbabilities;
money_edit.addEventListener("change", updateMoneyEidt);
updateProbabilities();

prob_slider.oninput = updateMoneys;
prob_edit.addEventListener("change", updateProbEidt);
updateMoneys();

for (var i = 0, row; row = prob_table.rows[i]; i++) {
   for (var j = 0, col; col = row.cells[j]; j++) {
     let cell = row.cells[j];
     let cell_id = cell.id;
     let s = cell_id.split("_");
     if (s.length == 3)
     {
      let r0 = coefficients[s[1] + "_0"];
      let r1 = coefficients[s[1] + "_1"];
      let b0 = coefficients[s[2] + "_0"];
      let b1 = coefficients[s[2] + "_1"];
      cell.addEventListener("click", function() {
        if (cell.bgColor) {
          let col = cell.bgColor;
          cell.bgColor = "";

          let index = ch.data.datasets.findIndex(x => x.borderColor == col);
          ch.data.datasets.splice(index, 1);

          index = ch.data.datasets.findIndex(x => x.borderColor == col);
          ch.data.datasets.splice(index, 1);

          delete selected_colors[col];
          ch.update();

        } else {
          let col = findColor();
          if (col) {
            let dataset = createDataset(r0,r1,b0,b1, col);
            dataset.label = towns[s[1]] + "x" + towns[s[2]];
            ch.data.datasets.push(dataset);
            let datasetPoints = createDatasetPoints(s[1], s[2], col);
            datasetPoints.label = towns[s[1]] + "x" + towns[s[2]];
            ch.data.datasets.push(datasetPoints);

            selected_colors[col] = dataset;
            cell.bgColor = col;
            ch.update();
          }
        }
        });
     }
   }  
}



// let coefficients_table = document.getElementById("coefficients");
// for (var i = 0, row; row = coefficients_table.rows[i]; i++) {
//    for (var j = 0, col; col = row.cells[j]; j++) {
//       let id = row.cells[j].id;
//       let c = coefficients[id];
//       if (c)
//         row.cells[j].innerHTML = c.toFixed(3);
//    }  
// }

let selected_colors = {};
function findColor() {
  for (let i = 0; i < colors.length; ++i) {
    if (!(colors[i] in selected_colors)) {
      return colors[i];
    }
  }
}

function createDataset(r0,r1,b0,b1,color) {
  let data = []; 
  for (let x = 0; x <= 20000; x += 100) {
    let m = (x - 10000) / 10000;
    let p = (1 / (1 + Math.exp(-(r0-b0)-(r1+b1)*m)));
    data.push({'x': x, 'y': p});
  }

  let dataset = {
    fill: false,
    pointRadius: 2,
    borderColor: color,
    data: data,
    //pointRadius: 12 
  };

  return dataset; 
}

function createDatasetPoints(r, b, color) {
  let data = []; 

  for (let i = 0; i < data_ponints.length; ++i) {
    if (data_ponints[i].red == r && data_ponints[i].blue == b) {
      let p = data_ponints[i].redWin ? 1 : 0;
      data.push({'x': data_ponints[i].redMoney, 'y': p});
    }
  }

  let dataset = {
    fill: false,
    borderColor: color,
//    backgroundColor: color,
    pointStyle: 'bubble',
    data: data,
    pointRadius: 10,
    showLine: false,
  };

  return dataset; 
}

window.resetZoom = function() {
  ch.resetZoom();
};

function createTable(id) {
  let table = document.createElement('table');

  var header = document.createElement('tr'); 
  var th = document.createElement('th'); 
  th.innerHTML = 'Red\\Blue';
  header.appendChild(th);
  for (const [headerShort, headerLong] of Object.entries(towns)) {
    th = document.createElement('th'); 
    th.innerHTML = headerLong;
    header.appendChild(th);
  }
  table.appendChild(header);

  for (const [redShort, redLong] of Object.entries(towns)) {
    var row = document.createElement('tr'); 

    var th = document.createElement('th'); 
    th.innerHTML = redLong;
    row.appendChild(th);

    for (const [blueShort, blueLong] of Object.entries(towns)) {    
      var td = document.createElement('td'); 
      td.append
      td.innerHTML = 0;
      td.id = id + "_" + redShort + "_" + blueShort;
      row.appendChild(td);
    }

    table.appendChild(row);
  }

  document.body.appendChild(table);
  table.id = id;

  return table
}