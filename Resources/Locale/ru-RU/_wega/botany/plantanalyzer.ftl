# Основные элементы интерфейса
plant-analyzer-component-no-seed = растение не найдено
plant-analyzer-component-health = Здоровье:
plant-analyzer-component-age = Возраст:
plant-analyzer-component-maturation = Созревание:
plant-analyzer-component-water = Вода:
plant-analyzer-component-nutrition = Питание:
plant-analyzer-component-toxins = Токсины:
plant-analyzer-component-pests = Вредители:
plant-analyzer-component-weeds = Сорняки:

# Статусы растений
plant-analyzer-component-alive = ЖИВОЙ
plant-analyzer-component-dead = МЁРТВЫЙ
plant-analyzer-component-unviable = НЕЖИЗНЕСПОСОБНЫЙ
plant-analyzer-component-mutating = МУТИРУЕТ
plant-analyzer-component-kudzu = КУДЗУ

# Химикаты в почве
plant-analyzer-soil = В этом {$holder} есть [color=white]{$chemicals}[/color], которые {$count ->
    [one]не был
    *[other]не были
} поглощены.
plant-analyzer-soil-empty = В этом {$holder} нет непоглощённых химикатов.

# Идеальные условия
plant-analyzer-component-environemt = Этому [color=green]{$seedName}[/color] требуется атмосфера с давлением [color=lightblue]{$kpa} кПа ± {$kpaTolerance} кПа[/color], температурой [color=lightsalmon]{$temp} K ± {$tempTolerance} K[/color] и уровнем освещения [color=white]{$lightLevel} ± {$lightTolerance}[/color].
plant-analyzer-component-environemt-void = Этот [color=green]{$seedName}[/color] должен расти [bolditalic]в вакууме космоса[/bolditalic] при уровне освещения [color=white]{$lightLevel} ± {$lightTolerance}[/color].
plant-analyzer-component-environemt-gas = Этому [color=green]{$seedName}[/color] требуется атмосфера, содержащая [bold]{$gases}[/bold], с давлением [color=lightblue]{$kpa} кПа ± {$kpaTolerance} кПа[/color], температурой [color=lightsalmon]{$temp} K ± {$tempTolerance} K[/color] и уровнем освещения [color=white]{$lightLevel} ± {$lightTolerance}[/color].

# Выход растения
plant-analyzer-output = {$yield ->
    [0]{$gasCount ->
        [0]Кажется, он только потребляет воду и питательные вещества.
        *[other]Кажется, он только превращает воду и питательные вещества в [bold]{$gases}[/bold].
    }
    *[other]У него [color=lightgreen]{$yield} {$potency}[/color]{$seedless ->
        [true]{" "}но [color=red]бессемянных[/color]
        *[false]{$nothing}
    }{" "}{$yield ->
        [one]цветок
        [few]цветка
        [many]цветков
        *[other]цветков
    }{" "}котор{$yield ->
        [one]ый
        *[other]ые
    }{ $gasCount ->
        [0]{$nothing}
        *[other]{$yield ->
            [one]{" "}выделяет
            *[other]{" "}выделяют
        }{" "}[bold]{$gases}[/bold] и
    }{" "}превратит{$yield ->
        [one]ся в {INDEFINITE($firstProduce)} [color=#a4885c]{$produce}[/color]
        *[other]ся в [color=#a4885c]{$producePlural}[/color]
    }.{$chemCount ->
        [0]{$nothing}
        *[other]{" "}В его стебле обнаружены следовые количества [color=white]{$chemicals}[/color].
    }
}

# Уровни мощности
plant-analyzer-potency-tiny = крошечный
plant-analyzer-potency-small = маленький
plant-analyzer-potency-below-average = ниже среднего
plant-analyzer-potency-average = средний
plant-analyzer-potency-above-average = выше среднего
plant-analyzer-potency-large = довольно большой
plant-analyzer-potency-huge = огромный
plant-analyzer-potency-gigantic = гигантский
plant-analyzer-potency-ludicrous = невероятно большой
plant-analyzer-potency-immeasurable = неизмеримо большой

# Печать и интерфейс
plant-analyzer-print = Печать
plant-analyzer-printout-missing = Н/Д
plant-analyzer-printout =
    {"[color=#9FED58][head=2]Отчёт анализатора растений[/head][/color]"}
    ──────────────────────────────
    {"[bullet/]"} Вид: {$seedName}
    {"    "}[bullet/] Жизнеспособен: {$viable ->
        [no][color=red]Нет[/color]
        [yes][color=green]Да[/color]
        *[other]{LOC("plant-analyzer-printout-missing")}
    }
    {"    "}[bullet/] Выносливость: {$endurance}
    {"    "}[bullet/] Продолжительность жизни: {$lifespan}
    {"    "}[bullet/] Продукт: [color=#a4885c]{$produce}[/color]
    {"    "}[bullet/] Кудзу: {$kudzu ->
        [no][color=green]Нет[/color]
        [yes][color=red]Да[/color]
        *[other]{LOC("plant-analyzer-printout-missing")}
    }
    {"[bullet/]"} Профиль роста:
    {"    "}[bullet/] Вода: [color=cyan]{$water}[/color]
    {"    "}[bullet/] Питание: [color=orange]{$nutrients}[/color]
    {"    "}[bullet/] Токсины: [color=yellowgreen]{$toxins}[/color]
    {"    "}[bullet/] Вредители: [color=magenta]{$pests}[/color]
    {"    "}[bullet/] Сорняки: [color=red]{$weeds}[/color]
    {"[bullet/]"} Профиль среды:
    {"    "}[bullet/] Состав: [bold]{$gasesIn}[/bold]
    {"    "}[bullet/] Давление: [color=lightblue]{$kpa} кПа ± {$kpaTolerance} кПа[/color]
    {"    "}[bullet/] Температура: [color=lightsalmon]{$temp} K ± {$tempTolerance} K[/color]
    {"    "}[bullet/] Освещение: [color=gray][bold]{$lightLevel} ± {$lightTolerance}[/bold][/color]
    {"[bullet/]"} Цветы: {$yield ->
        [-1]{LOC("plant-analyzer-printout-missing")}
        [0][color=red]0[/color]
        *[other][color=lightgreen]{$yield} {$potency}[/color]
    }
    {"[bullet/]"} Семена: {$seeds ->
        [no][color=red]Нет[/color]
        [yes][color=green]Да[/color]
        *[other]{LOC("plant-analyzer-printout-missing")}
    }
    {"[bullet/]"} Химикаты: [color=gray][bold]{$chemicals}[/bold][/color]
    {"[bullet/]"} Выделения: [bold]{$gasesOut}[/bold]

# Новые элементы улучшенного интерфейса
plant-analyzer-scan-active = СКАНИРОВАНИЕ
plant-analyzer-scan-inactive = НЕАКТИВНО
plant-analyzer-no-data = НЕТ ДАННЫХ
plant-analyzer-no-plant = Растение не обнаружено
plant-analyzer-unknown-container = Неизвестный контейнер
plant-analyzer-tray-conditions = Состояние лотка
plant-analyzer-soil-chemicals = Химикаты в почве
plant-analyzer-ideal-environment = Идеальные условия
plant-analyzer-plant-output = Выход растения
plant-analyzer-print-report = Печать отчёта
plant-analyzer-temperature = Температура
plant-analyzer-pressure = Давление
plant-analyzer-light = Освещение
plant-analyzer-required-gases = Требуемые газы
plant-analyzer-yield = Урожай
plant-analyzer-potency = Мощность
plant-analyzer-produces = Производит
plant-analyzer-chemicals = Химикаты
plant-analyzer-exudes = Выделяет газы
plant-analyzer-seeds = Семена
plant-analyzer-seedless = Бессемянное
plant-analyzer-produces-seeds = Производит семена

# Всплывающие сообщения
plant-analyzer-popup-scan-target = Производится сканирование {$target}
plant-analyzer-printer-not-ready = Принтер ещё не готов
