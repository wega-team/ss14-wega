cmd-achievements_grant-desc = Выдать ачивку игроку
cmd-achievements_grant-help = Использование: { $command } <имя игрока> <id ачивки>
cmd-achievements_grant-error-args = Неверное количество аргументов. Использование: achievements_grant <имя игрока> <id ачивки>
cmd-achievements_grant-error-player = Не удалось найти игрока: { $username }
cmd-achievements_grant-error-achievement = Ачивка с ID { $id } не найдена
cmd-achievements_grant-success = Ачивка { $achievement } выдана игроку { $username }
cmd-achievements_grant-arg-user = Имя игрока
cmd-achievements_grant-arg-achievement = ID ачивки или название из enum
cmd-achievements_grant-already-has = Игрок { $username } уже имеет эту ачивку.

cmd-achievements_revoke-desc = Забрать ачивку у игрока
cmd-achievements_revoke-help = Использование: { $command } <имя игрока> <id ачивки>
cmd-achievements_revoke-error-args = Неверное количество аргументов. Использование: achievements_revoke <имя игрока> <id ачивки>
cmd-achievements_revoke-error-player = Не удалось найти игрока: { $username }
cmd-achievements_revoke-error-achievement = Ачивка с ID { $id } не найдена
cmd-achievements_revoke-success = Ачивка { $achievement } забрана у игрока { $username }
cmd-achievements_revoke-arg-user = Имя игрока
cmd-achievements_revoke-arg-achievement = ID ачивки или название из enum
cmd-achievements_revoke-not-has = У игрока { $username } нет этой ачивки.

cmd-achievements_clear-desc = Очистить все ачивки игрока
cmd-achievements_clear-help = Использование: { $command } <имя игрока>
cmd-achievements_clear-error-args = Неверное количество аргументов. Использование: achievements_clear <имя игрока>
cmd-achievements_clear-error-player = Не удалось найти игрока: { $username }
cmd-achievements_clear-success = Все ачивки ({ $count }) очищены у игрока { $username }
cmd-achievements_clear-arg-user = Имя игрока

cmd-achievements_list-desc = Показать ачивки игрока
cmd-achievements_list-help = Использование: { $command } <имя игрока>
cmd-achievements_list-error-args = Неверное количество аргументов. Использование: achievements_list <имя игрока>
cmd-achievements_list-error-player = Не удалось найти игрока: { $username }
cmd-achievements_list-no = У игрока нет ачивок
cmd-achievements_list-header = Ачивки игрока { $username }:
cmd-achievements_list-entry = { $name } (ID: { $id })
cmd-achievements_list-arg-user = Имя игрока
cmd-achievements_list-entry-no-prototype = ID: { $enum } (прототип не найден)

cmd-achievements_grantall-desc = Выдать все ачивки игроку
cmd-achievements_grantall-help = Использование: { $command } <имя игрока>
cmd-achievements_grantall-error-args = Неверное количество аргументов. Использование: achievements_grantall <имя игрока>
cmd-achievements_grantall-error-player = Не удалось найти игрока: { $username }
cmd-achievements_grantall-success = Игроку { $username } выдано { $granted } ачивок из { $total }.
cmd-achievements_grantall-arg-user = Имя игрока

## CompletionHelper
comp-help-session = Имя игрока
comp-help-achievement-id = ID ачивки
